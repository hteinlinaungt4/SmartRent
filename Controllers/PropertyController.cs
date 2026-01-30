using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRent.Data;
using SmartRent.Dto;
using SmartRent.Model;

namespace SmartRent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _environment;

        public PropertyController(DataContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ---------------------------------------------------------
        // 1. GET ALL: api/property
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyResponseDto>>> GetProperties()
        {
            return await _context.Properties
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
                    CategoryName = p.Category != null ? p.Category.Name : "General",
                    OwnerName = p.User != null ? p.User.Username : "Unknown",
                    TrustScore = p.User != null ? p.User.TrustScore : 0,
                    ThumbnailUrl = p.Images.Any(img => img.IsThumbnail)
                                   ? p.Images.First(img => img.IsThumbnail).ImageUrl
                                   : (p.Images.Any() ? p.Images.First().ImageUrl : "/uploads/default.png")
                })
                .ToListAsync();
        }

        // ---------------------------------------------------------
        // 2. GET BY ID: api/property/{id}
        // ---------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<Property>> GetProperty(Guid id)
        {
            var property = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return NotFound();
            return property;
        }

        // ---------------------------------------------------------
        // 3. POST: api/property (Image Upload ပါဝင်သည်)
        // ---------------------------------------------------------
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Property>> PostProperty([FromForm] CreatePropertyDto dto)
        {
            // User ID ရယူခြင်း
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized("User not found in token.");
            var userId = Guid.Parse(userIdString);

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

            // Image Upload Logic (Fixing Path.Combine Error)
            if (dto.Images != null && dto.Images.Count > 0)
            {
                // WebRootPath null ဖြစ်နေရင် manual ရှာမယ်
                string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "uploads", "properties");

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var file in dto.Images)
                {
                    string uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    property.Images.Add(new PropertyImage
                    {
                        Id = Guid.NewGuid(),
                        ImageUrl = $"/uploads/properties/{uniqueFileName}",
                        IsThumbnail = property.Images.Count == 0 // ပထမဆုံးပုံကို Thumbnail လုပ်မယ်
                    });
                }
            }

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // CreatedAtAction မှာ property (Entity) ကို တိုက်ရိုက်မပြန်ဘဲ 
            // လိုအပ်တဲ့ field တွေပဲပါတဲ့ object သစ်တစ်ခုအနေနဲ့ ပြန်ပေးလိုက်ပါ
            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, new
            {
                Id = property.Id,
                Title = property.Title,
                Price = property.Price,
                Images = property.Images.Select(img => new { img.ImageUrl, img.IsThumbnail })
            });
        }

        // ---------------------------------------------------------
        // 4. PUT: api/property/{id}
        // ---------------------------------------------------------
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProperty(Guid id, [FromForm] CreatePropertyDto dto)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null) return NotFound();

            // ပိုင်ရှင် ဟုတ်မဟုတ် စစ်ဆေးခြင်း
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (property.UserId != userId) return Forbid();

            property.Title = dto.Title;
            property.Description = dto.Description;
            property.Price = dto.Price;
            property.Township = dto.Township;
            property.CategoryId = dto.CategoryId;
            property.Beds = dto.Beds;
            property.Baths = dto.Baths;
            property.Sqft = dto.Sqft;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropertyExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // ---------------------------------------------------------
        // 5. DELETE: api/property/{id}
        // ---------------------------------------------------------
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(Guid id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return NotFound();

            // ပိုင်ရှင် ဟုတ်မဟုတ် စစ်ဆေးခြင်း
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (property.UserId != userId) return Forbid();

            // ပုံတွေကို Disk ပေါ်ကပါ ဖျက်ခြင်း
            string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            foreach (var img in property.Images)
            {
                var filePath = Path.Combine(webRootPath, img.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PropertyExists(Guid id) => _context.Properties.Any(e => e.Id == id);
    }
}