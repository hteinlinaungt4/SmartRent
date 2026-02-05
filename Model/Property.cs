using System.ComponentModel.DataAnnotations;

namespace SmartRent.Model
{
    public class Property
    {
        public Guid Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string Township { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int Beds { get; set; }
        public int Baths { get; set; }
        public int Sqft { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationships
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }
        public List<PropertyImage> Images { get; set; } = new();
    }
}