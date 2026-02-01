using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRent.Data;
using SmartRent.Dto;
using SmartRent.Model;
using SmartRent.Service;

namespace SmartRent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IImageService _imageService;

        public PropertyController(DataContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        // Base URL ယူရန် Helper
        private string GetBaseUrl() => $"{Request.Scheme}://{Request.Host}";

        // 1. GET ALL
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyResponseDto>>> GetProperties()
        {
            var baseUrl = GetBaseUrl();
            return await _context.Properties
                .Include(p => p.Category).Include(p => p.User).Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PropertyResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Township = p.Township,
                    Beds = p.Beds,
                    Baths = p.Baths,
                    Sqft = p.Sqft,
                    CategoryName = p.Category != null ? p.Category.Name : "General",
                    OwnerName = p.User != null ? p.User.Username : "Unknown",
                    TrustScore = p.User != null ? p.User.TrustScore : 0,
                    ThumbnailUrl = p.Images.Any(i => i.IsThumbnail)
                                   ? baseUrl + p.Images.First(i => i.IsThumbnail).ImageUrl
                                   : (p.Images.Any() ? baseUrl + p.Images.First().ImageUrl : baseUrl + "/uploads/default.png")
                }).ToListAsync();
        }

        // 2. GET BY ID
        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDetailResponseDto>> GetProperty(Guid id)
        {
            var baseUrl = GetBaseUrl();
            var property = await _context.Properties
                .Include(p => p.Category).Include(p => p.Images).Include(p => p.User)
                .Where(p => p.Id == id)
                .Select(p => new PropertyDetailResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    Township = p.Township,
                    Beds = p.Beds,
                    Baths = p.Baths,
                    Sqft = p.Sqft,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    CategoryName = p.Category != null ? p.Category.Name : "General",
                    CategoryId = p.CategoryId.ToString(),
                    OwnerName = p.User != null ? p.User.Username : "Unknown",
                    OwnerId = p.User != null ? p.User.Id.ToString() : string.Empty,
                    TrustScore = p.User != null ? p.User.TrustScore : 0,
                    OwnerPhone = p.User != null ? p.User.Phone : null,
                    Images = p.Images.Select(img => new ImageDto
                    {
                        Id = img.Id,
                        ImageUrl = baseUrl + img.ImageUrl,
                        IsThumbnail = img.IsThumbnail
                    }).ToList()
                }).FirstOrDefaultAsync();

            return property == null ? NotFound() : Ok(property);
        }

        // 3. POST
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> PostProperty([FromForm] CreatePropertyDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var property = new Property
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Township = dto.Township,
                Beds = dto.Beds,
                Baths = dto.Baths,
                Sqft = dto.Sqft,
                CategoryId = dto.CategoryId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Images = new List<PropertyImage>()
            };

            if (dto.Images != null)
            {
                foreach (var file in dto.Images)
                {
                    var relativeUrl = await _imageService.SaveImageAsync(file, "properties");
                    property.Images.Add(new PropertyImage
                    {
                        Id = Guid.NewGuid(),
                        ImageUrl = relativeUrl,
                        IsThumbnail = property.Images.Count == 0
                    });
                }
            }

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, new { id = property.Id });
        }

        // 4. PUT (Update)
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProperty(Guid id, [FromForm] UpdatePropertyDto dto)
        {
            var property = await _context.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (property == null) return NotFound();

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (property.UserId != userId) return Forbid();

            // Update Fields
            property.Title = dto.Title;
            property.Description = dto.Description;
            property.Price = dto.Price;
            property.Township = dto.Township;
            property.CategoryId = dto.CategoryId;
            property.Beds = dto.Beds;
            property.Baths = dto.Baths;
            property.Sqft = dto.Sqft;

            // Delete Images (Physical File + DB Record)
            if (dto.DeletedImageIds != null && dto.DeletedImageIds.Any())
            {
                var imagesToRemove = property.Images.Where(i => dto.DeletedImageIds.Contains(i.Id)).ToList();
                foreach (var img in imagesToRemove)
                {
                    _imageService.DeleteImage(img.ImageUrl); // physical ဖျက်မယ်
                    _context.PropertyImages.Remove(img);     // DB က ဖျက်မယ်
                }
            }

            // Add New Images
            if (dto.Images != null)
            {
                foreach (var file in dto.Images)
                {
                    var relativeUrl = await _imageService.SaveImageAsync(file, "properties");
                    property.Images.Add(new PropertyImage
                    {
                        Id = Guid.NewGuid(),
                        ImageUrl = relativeUrl,
                        IsThumbnail = false
                    });
                }
            }

            // အကယ်၍ Thumbnail ရှိမနေရင် ပထမဆုံးပုံကို Thumbnail ပေးမယ်
            if (property.Images.Any() && !property.Images.Any(i => i.IsThumbnail))
                property.Images.First().IsThumbnail = true;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. DELETE
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(Guid id)
        {
            var property = await _context.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (property == null) return NotFound();

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (property.UserId != userId) return Forbid();

            // ဆိုင်ရာ ပုံတွေအကုန်လုံးကို physical ဖျက်ပစ်မယ်
            foreach (var img in property.Images)
            {
                _imageService.DeleteImage(img.ImageUrl);
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}