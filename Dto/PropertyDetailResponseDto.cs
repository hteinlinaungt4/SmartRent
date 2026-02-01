namespace SmartRent.Dto
{
    public class PropertyDetailResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Price { get; set; } = string.Empty;
        public string Township { get; set; } = string.Empty;
        public int Beds { get; set; }
        public int Baths { get; set; }
        public int Sqft { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }

        // Category Info
        public string CategoryName { get; set; } = string.Empty;

        // Owner Info (Sensitive data တွေ မပါတော့ဘူး)
        public string OwnerName { get; set; } = string.Empty;
        public int TrustScore { get; set; }
        public string? OwnerPhone { get; set; }

        // Images List
        public List<ImageDto> Images { get; set; } = new();
    }


    public class ImageDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsThumbnail { get; set; }
    }
}
