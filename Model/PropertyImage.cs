using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartRent.Model
{
    public class PropertyImage
    {
        public Guid Id { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsThumbnail { get; set; } = false;

        // Best Practice: Explicitly define the Foreign Key for EF Core performance
        public Guid PropertyId { get; set; }

        [JsonIgnore]
        [ForeignKey("PropertyId")] // Ensures EF Core maps the relationship correctly
        public Property? Property { get; set; }
    }
}