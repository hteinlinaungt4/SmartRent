using SmartRent.Dto;
using SmartRent.Model;

namespace SmartRent.Interface
{
    public interface IPropertyService
    {
        Task<IEnumerable<PropertyResponseDto>> GetAllPropertiesAsync();
        Task<PropertyDetailResponseDto?> GetPropertyByIdAsync(Guid id);
        Task<PropertyDetailResponseDto> CreatePropertyAsync(CreatePropertyDto dto, Guid userId);
        Task<bool> UpdatePropertyAsync(Guid id, UpdatePropertyDto dto, Guid userId);
        Task<bool> DeletePropertyAsync(Guid id, Guid userId);
    }
}
