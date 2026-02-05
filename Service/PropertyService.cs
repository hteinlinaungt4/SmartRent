using Microsoft.EntityFrameworkCore;
using SmartRent.Data;
using SmartRent.Dto;
using SmartRent.Interface;
using SmartRent.Model;

namespace SmartRent.Service
{
    public class PropertyService : IPropertyService
    {
        private readonly DataContext _context;
        private readonly IImageService _imageService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PropertyService(DataContext context, IImageService imageService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _imageService = imageService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string BaseUrl
        {
            get
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                return request != null ? $"{request.Scheme}://{request.Host}" : string.Empty;
            }
        }

        public async Task<(IEnumerable<PropertyResponseDto> items, int totalCount, int totalPages, int currentPage)> GetAllPropertiesAsync(int page = 1, int pageSize = 10)
        {
            var totalCount = await _context.Properties.AsNoTracking().CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            var items = await _context.Properties
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PropertyResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Township = p.Township,
                    Address = p.Address,
                    Beds = p.Beds,
                    Baths = p.Baths,
                    Sqft = p.Sqft,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                    OwnerName = p.User != null ? p.User.Username : string.Empty,
                    TrustScore = p.User != null ? p.User.TrustScore : 0,
                    ThumbnailUrl = p.Images.Where(i => i.IsThumbnail)
                                    .Select(i => BaseUrl + i.ImageUrl)
                                    .FirstOrDefault() ?? (BaseUrl + "/uploads/default.png")
                })
                .ToListAsync();

            return (items, totalCount, totalPages, page);
        }

        public async Task<PropertyDetailResponseDto?> GetPropertyByIdAsync(Guid id)
        {
            var property = await _context.Properties
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return null;

            return MapToDetailDto(property);
        }

        public async Task<PropertyDetailResponseDto> CreatePropertyAsync(CreatePropertyDto dto, Guid userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var property = new Property
                    {
                        Id = Guid.NewGuid(),
                        Title = dto.Title,
                        Description = dto.Description,
                        Price = dto.Price,
                        Township = dto.Township,
                        Address = dto.Address,
                        Beds = dto.Beds,
                        Baths = dto.Baths,
                        Sqft = dto.Sqft,
                        CategoryId = dto.CategoryId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        IsAvailable = true
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
                                IsThumbnail = property.Images.Count == 0
                            });
                        }
                    }

                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Refetch to get navigation properties
                    var result = await _context.Properties
                        .Include(p => p.Category)
                        .Include(p => p.User)
                        .Include(p => p.Images)
                        .FirstAsync(p => p.Id == property.Id);

                    return MapToDetailDto(result);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<bool> UpdatePropertyAsync(Guid id, UpdatePropertyDto dto, Guid userId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Step 1: Check ownership without tracking
                    var ownerCheck = await _context.Properties
                        .AsNoTracking()
                        .Where(p => p.Id == id)
                        .Select(p => new { p.UserId })
                        .FirstOrDefaultAsync();

                    if (ownerCheck == null)
                    {
                        _httpContextAccessor.HttpContext!.Items["UpdateError"] = "Property not found.";
                        return false;
                    }

                    if (ownerCheck.UserId != userId)
                    {
                        _httpContextAccessor.HttpContext!.Items["UpdateError"] = "Authorization failed: You do not own this property.";
                        return false;
                    }

                    // Step 2: Load property fresh with tracking for update
                    var property = await _context.Properties
                        .Include(p => p.Images)
                        .FirstAsync(p => p.Id == id);

                    // Step 3: Update Basic Info
                    property.Title = dto.Title;
                    property.Description = dto.Description;
                    property.Price = dto.Price;
                    property.Township = dto.Township;
                    property.Address = dto.Address;
                    property.Beds = dto.Beds;
                    property.Baths = dto.Baths;
                    property.Sqft = dto.Sqft;
                    property.CategoryId = dto.CategoryId;
                    property.IsAvailable = dto.IsAvailable;
                    property.IsFeatured = dto.IsFeatured;

                    // Step 4: Handle Image Deletions
                    var removedUrls = new List<string>();
                    if (dto.DeletedImageIds != null && dto.DeletedImageIds.Count > 0)
                    {
                        var imagesToRemove = property.Images
                            .Where(img => dto.DeletedImageIds.Contains(img.Id))
                            .ToList();

                        foreach (var img in imagesToRemove)
                        {
                            removedUrls.Add(img.ImageUrl);
                            _context.PropertyImages.Remove(img);
                        }
                    }

                    // Save deletions first to get accurate count
                    await _context.SaveChangesAsync();

                    // Step 5: Get remaining images count
                    var remainingImagesCount = await _context.PropertyImages
                        .Where(pi => pi.PropertyId == id)
                        .CountAsync();

                    // Step 6: Handle Image Additions
                    if (dto.Images != null && dto.Images.Count > 0)
                    {
                        foreach (var file in dto.Images)
                        {
                            var url = await _imageService.SaveImageAsync(file, "properties");
                            var newImage = new PropertyImage
                            {
                                Id = Guid.NewGuid(),
                                ImageUrl = url,
                                PropertyId = property.Id,
                                IsThumbnail = remainingImagesCount == 0
                            };
                            _context.PropertyImages.Add(newImage);
                            remainingImagesCount++;
                        }
                    }

                    // Step 7: Ensure Thumbnail Integrity
                    var allImages = await _context.PropertyImages
                        .Where(pi => pi.PropertyId == id)
                        .ToListAsync();

                    if (allImages.Count > 0 && !allImages.Any(i => i.IsThumbnail))
                    {
                        allImages.OrderBy(i => i.Id).First().IsThumbnail = true;
                    }

                    // Step 8: Save Changes
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Step 9: Background Cleanup
                    _ = Task.Run(() =>
                    {
                        foreach (var url in removedUrls)
                        {
                            try { _imageService.DeleteImage(url); } catch { }
                        }
                    });

                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _httpContextAccessor.HttpContext!.Items["UpdateError"] = "Concurrency Error: " + ex.Message;
                    return false;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _httpContextAccessor.HttpContext!.Items["UpdateError"] = "Error: " + ex.Message;
                    return false;
                }
            });
        }

        public async Task<bool> DeletePropertyAsync(Guid id, Guid userId)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null || property.UserId != userId) return false;

            var urls = property.Images.Select(i => i.ImageUrl).ToList();

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            foreach (var url in urls) _imageService.DeleteImage(url);

            return true;
        }

        private PropertyDetailResponseDto MapToDetailDto(Property p)
        {
            return new PropertyDetailResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                Township = p.Township,
                Address = p.Address,
                Beds = p.Beds,
                Baths = p.Baths,
                Sqft = p.Sqft,
                IsAvailable = p.IsAvailable,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                OwnerId = p.UserId,
                OwnerName = p.User?.Username ?? string.Empty,
                OwnerPhone = p.User?.Phone,
                TrustScore = p.User?.TrustScore ?? 0,
                Images = p.Images.Select(i => new PropertyImageDto
                {
                    Id = i.Id,
                    ImageUrl = BaseUrl + i.ImageUrl,
                    IsThumbnail = i.IsThumbnail
                }).ToList()
            };
        }
    }
}
