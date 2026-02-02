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
    [ApiController]
    [Route("api/properties")]
    public class PropertyController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IImageService _imageService;

        public PropertyController(DataContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        // ==================================================
        // GET: api/properties
        // ==================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyResponseDto>>> GetAll()
        {
            return await _context.Properties
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Images)
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
                    CategoryName = p.Category!.Name,
                    OwnerName = p.User!.Username,
                    TrustScore = p.User!.TrustScore,
                    ThumbnailUrl = p.Images.Any(i => i.IsThumbnail)
                        ? BaseUrl + p.Images.First(i => i.IsThumbnail).ImageUrl
                        : BaseUrl + "/uploads/default.png"
                })
                .ToListAsync();
        }

        // ==================================================
        // GET: api/properties/{id}
        // ==================================================
        [HttpGet("{id}", Name = "GetPropertyById")]
        public async Task<ActionResult<PropertyDetailResponseDto>> GetById(Guid id)
        {
            var property = await _context.Properties
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound();

            return Ok(new PropertyDetailResponseDto
            {
                Id = property.Id,
                Title = property.Title,
                Description = property.Description,
                Price = property.Price,
                Township = property.Township,
                Beds = property.Beds,
                Baths = property.Baths,
                Sqft = property.Sqft,
                IsAvailable = property.IsAvailable,
                CreatedAt = property.CreatedAt,
                CategoryId = property.CategoryId.ToString(),
                CategoryName = property.Category!.Name,
                OwnerId = property.UserId.ToString(),
                OwnerName = property.User!.Username,
                OwnerPhone = property.User.Phone,
                TrustScore = property.User.TrustScore,
                Images = property.Images.Select(i => new ImageDto
                {
                    Id = i.Id,
                    ImageUrl = BaseUrl + i.ImageUrl,
                    IsThumbnail = i.IsThumbnail
                }).ToList()
            });
        }

        // ==================================================
        // POST: api/properties
        // ==================================================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreatePropertyDto dto)
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

            if (dto.Images?.Any() == true)
            {
                foreach (var file in dto.Images)
                {
                    var url = await _imageService.SaveImageAsync(file, "properties");
                    property.Images.Add(new PropertyImage
                    {
                        Id = Guid.NewGuid(),
                        ImageUrl = url,
                        IsThumbnail = !property.Images.Any()
                    });
                }
            }

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // Corrected: include the value (response body)
            var resultDto = new PropertyDetailResponseDto
            {
                Id = property.Id,
                Title = property.Title,
                Description = property.Description,
                Price = property.Price,
                Township = property.Township,
                Beds = property.Beds,
                Baths = property.Baths,
                Sqft = property.Sqft,
                IsAvailable = property.IsAvailable,
                CreatedAt = property.CreatedAt,
                CategoryId = property.CategoryId.ToString(),
                CategoryName = _context.Categories.Find(property.CategoryId)?.Name ?? string.Empty,
                OwnerId = property.UserId.ToString(),
                OwnerName = User.Identity?.Name ?? string.Empty,
                TrustScore = 0,
                Images = property.Images.Select(i => new ImageDto
                {
                    Id = i.Id,
                    ImageUrl = BaseUrl + i.ImageUrl,
                    IsThumbnail = i.IsThumbnail
                }).ToList()
            };

            return CreatedAtRoute(
                routeName: "GetPropertyById",
                routeValues: new { id = property.Id },
                value: resultDto
            );
        }


        // ==================================================
        // PUT: api/properties/{id}
        // ==================================================
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdatePropertyDto dto)
        {
            // Load property with images using tracking
            var property = await _context.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound();

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (property.UserId != userId)
                return Forbid();

            // -----------------------------
            // Update scalar fields
            // -----------------------------
            property.Title = dto.Title;
            property.Description = dto.Description;
            property.Price = dto.Price;
            property.Township = dto.Township;
            property.Beds = dto.Beds;
            property.Baths = dto.Baths;
            property.Sqft = dto.Sqft;
            property.CategoryId = dto.CategoryId;

            var removedImageUrls = new List<string>();

            // -----------------------------
            // Remove images explicitly
            // -----------------------------
            if (dto.DeletedImageIds?.Any() == true)
            {
                // Load the images from DB for deletion
                var imagesToRemove = await _context.PropertyImages
                    .Where(i => dto.DeletedImageIds.Contains(i.Id) && i.PropertyId == id)
                    .ToListAsync();

                foreach (var img in imagesToRemove)
                {
                    removedImageUrls.Add(img.ImageUrl);
                    _context.PropertyImages.Remove(img); // EF tracks deletion
                }
            }

            // -----------------------------
            // Add new images
            // -----------------------------
            if (dto.Images?.Any() == true)
            {
                foreach (var file in dto.Images)
                {
                    var url = await _imageService.SaveImageAsync(file, "properties");
                    var newImage = new PropertyImage
                    {
                        Id = Guid.NewGuid(),
                        ImageUrl = url,
                        IsThumbnail = false,
                        PropertyId = property.Id
                    };
                    _context.PropertyImages.Add(newImage);
                }
            }

            // -----------------------------
            // Ensure exactly one thumbnail
            // -----------------------------
            var allImages = await _context.PropertyImages
                .Where(i => i.PropertyId == property.Id)
                .ToListAsync();

            if (allImages.Any())
            {
                foreach (var img in allImages)
                    img.IsThumbnail = false;

                allImages.First().IsThumbnail = true;
            }

            // -----------------------------
            // Save changes in a single transaction
            // -----------------------------
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "The property was modified by someone else. Please reload and try again." });
            }

            // -----------------------------
            // Delete physical files after DB commit
            // -----------------------------
            foreach (var url in removedImageUrls)
            {
                try { _imageService.DeleteImage(url); } catch { }
            }

            return Ok(new { message = "Property updated successfully" });
        }


        // ==================================================
        // DELETE: api/properties/{id}
        // ==================================================
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound();

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (property.UserId != userId)
                return Forbid();

            var imageUrls = property.Images.Select(i => i.ImageUrl).ToList();

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            foreach (var url in imageUrls)
            {
                try { _imageService.DeleteImage(url); } catch { }
            }

            return NoContent();
        }
    }
}
