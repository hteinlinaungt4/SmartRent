using System.ComponentModel.DataAnnotations;

namespace SmartRent.Model
{
    public class Category
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., Condo, Apartment

        public string? IconName { get; set; } // e.g., "business-outline" (Ionicons)

        // Navigation Property: Category တစ်ခုအောက်မှာ Property တွေ အများကြီးရှိနိုင်သည်
        public List<Property> Properties { get; set; } = new();
    }
}
