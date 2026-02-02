using System.ComponentModel.DataAnnotations;

namespace SmartRent.Model
{
    public class Category
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Name { get; set; } = string.Empty; 

        public string? IconName { get; set; } 
        public List<Property> Properties { get; set; } = new();
    }
}
