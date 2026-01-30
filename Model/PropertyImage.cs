namespace SmartRent.Model
{
    public class PropertyImage
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsThumbnail { get; set; } = false;

        public Guid PropertyId { get; set; }
        public Property? Property { get; set; }
    }
}
