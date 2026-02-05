using SmartRent.Model;

namespace SmartRent.Dto
{
    public class PropertyDetailResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Township { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int Beds { get; set; }
        public int Baths { get; set; }
        public int Sqft { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }

        // Category Info
        public string CategoryName { get; set; } = string.Empty;
        // Best Practice: Keep IDs as Guids in DTOs if they are Guids in the DB
        public Guid CategoryId { get; set; }

        // Owner Info
        public string OwnerName { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public int TrustScore { get; set; }
        public string? OwnerPhone { get; set; }

        // Images List
        public List<PropertyImageDto> Images { get; set; } = new();
    }
}