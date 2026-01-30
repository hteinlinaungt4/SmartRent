using System.Security.Claims;
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

        // 3. POST: api/properties (ပုံတွေကို Folder ထဲသိမ်းပြီး Path ကို DB ထဲထည့်ခြင်း)
        [HttpPost]
        public async Task<ActionResult<Property>> PostProperty([FromForm] CreatePropertyDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            var property = new Property
            {
                Id = Guid.NewGuid(), // ID ကို ကြိုသတ်မှတ်လိုက်မယ် (Folder နာမည်အတွက် သုံးလို့ရအောင်)
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

            // --- Image Upload Logic ---
            if (dto.Images != null && dto.Images.Count > 0)
            {
                // wwwroot/uploads/properties ဆိုတဲ့ path ကို ယူမယ်
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "properties");

                // Folder မရှိရင် ဆောက်မယ်
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var file in dto.Images)
                {
                    // File Name ကို Unique ဖြစ်အောင် Guid နဲ့ ပေါင်းမယ် (ဥပမာ - a123_livingroom.jpg)
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Project Folder ထဲကို တကယ်သိမ်းလိုက်တာ
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    // DB မှာ သိမ်းမယ့် Relative Path (Frontend က လှမ်းခေါ်ရမယ့် path)
                    string dbImagePath = "/uploads/properties/" + uniqueFileName;

                    property.Images.Add(new PropertyImage
                    {
                        ImageUrl = dbImagePath,
                        IsThumbnail = property.Images.Count == 0 // ပထမဆုံးပုံကို Thumbnail ပေးမယ်
                    });
                }
            }

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, property);
        }

        // GET, PUT, DELETE တွေကတော့ အရင်အတိုင်းပဲ ထားလို့ရပါတယ် (Guid id နဲ့ ပြင်ထားပါ)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyResponseDto>>> GetProperties()
        {
            return await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Images)
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
                    TrustScore = p.User.TrustScore,
                    ThumbnailUrl = p.Images.FirstOrDefault(img => img.IsThumbnail)!.ImageUrl
                                   ?? p.Images.FirstOrDefault()!.ImageUrl
                })
                .ToListAsync();
        }

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
    }
}