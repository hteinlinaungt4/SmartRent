using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRent.Data;
using SmartRent.Dto;
using SmartRent.Interface;
using SmartRent.Model;
using SmartRent.Service;

namespace SmartRent.Controllers
{
    [ApiController]
    [Route("api/properties")]
    public class PropertyController : ControllerBase
    {
        private readonly IPropertyService _propertyService;

        public PropertyController(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (items, totalCount, totalPages, currentPage) = await _propertyService.GetAllPropertiesAsync(page, pageSize);
            
            var response = new
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                HasNextPage = currentPage < totalPages,
                HasPreviousPage = currentPage > 1
            };
            
            return Ok(response);
        }

        [HttpGet("{id}", Name = "GetPropertyById")]
        public async Task<ActionResult<PropertyDetailResponseDto>> GetById(Guid id)
        {
            var property = await _propertyService.GetPropertyByIdAsync(id);
            if (property == null) return NotFound();

            return Ok(property);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreatePropertyDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId)) return Unauthorized();

            var result = await _propertyService.CreatePropertyAsync(dto, userId);

            return CreatedAtRoute("GetPropertyById", new { id = result.Id }, result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdatePropertyDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId)) return Unauthorized();

            var success = await _propertyService.UpdatePropertyAsync(id, dto, userId);

            if (!success)
            {
                var errorMessage = HttpContext.Items["UpdateError"] as string ?? "Update failed.";
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "Property updated successfully" });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId)) return Unauthorized();

            var success = await _propertyService.DeletePropertyAsync(id, userId);

            if (!success) return NotFound();

            return NoContent();
        }
    }
}