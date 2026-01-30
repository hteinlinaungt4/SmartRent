using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRent.Data;
using SmartRent.Model;

namespace SmartRent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly DataContext _context;

        public CategoriesController(DataContext context)
        {
            _context = context;
        }


        // 1. GET: api/categories (အားလုံးကို ဆွဲထုတ်ခြင်း)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        // 2. GET: api/categories/5 (တစ်ခုချင်းစီ အသေးစိတ်ကြည့်ခြင်း)
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) return NotFound("Category ရှာမတွေ့ပါ။");

            return category;
        }

        // 3. POST: api/categories (Category အသစ်ထည့်ခြင်း)
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // 4. PUT: api/categories/5 (ပြင်ဆင်ခြင်း)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, Category category)
        {
            if (id != category.Id) return BadRequest();

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // 5. DELETE: api/categories/5 (ဖျက်သိမ်းခြင်း)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Category ထဲမှာ Property တွေ ရှိနေရင် ဖျက်လို့မရအောင် ကာကွယ်ခြင်း (Optional)
            var hasProperties = await _context.Properties.AnyAsync(p => p.CategoryId == id);
            if (hasProperties) return BadRequest("ဒီ Category ထဲမှာ အခန်းများ ရှိနေသေးသဖြင့် ဖျက်၍မရပါ။");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(Guid id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
