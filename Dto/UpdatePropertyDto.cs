namespace SmartRent.Dto
{
    public class UpdatePropertyDto : CreatePropertyDto
    {
        public bool IsAvailable { get; set; }
        public bool IsFeatured { get; set; }
        public List<Guid>? DeletedImageIds { get; set; } = new();
    }
}