using System.Text.Json.Serialization;

namespace SmartRent.Dto
{
    public class PropertyResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Price { get; set; }
        public string Township { get; set; } = string.Empty;
        public int Beds { get; set; }
        public int Baths { get; set; }
        public int Sqft { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int TrustScore { get; set; }
    }
}
