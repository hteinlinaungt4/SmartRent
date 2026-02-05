using SmartRent.Dto;
using SmartRent.Model;

namespace SmartRent.Interface
{
    public interface IPropertyService
    {
        Task<(IEnumerable<PropertyResponseDto> items, int totalCount, int totalPages, int currentPage)> GetAllPropertiesAsync(int page = 1, int pageSize = 10);
        Task<PropertyDetailResponseDto?> GetPropertyByIdAsync(Guid id);
        Task<PropertyDetailResponseDto> CreatePropertyAsync(CreatePropertyDto dto, Guid userId);
        Task<bool> UpdatePropertyAsync(Guid id, UpdatePropertyDto dto, Guid userId);
        Task<bool> DeletePropertyAsync(Guid id, Guid userId);
    }
}
