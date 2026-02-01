namespace SmartRent.Dto
{
    public class UpdatePropertyDto : CreatePropertyDto
    {
        public List<Guid>? DeletedImageIds { get; set; }
    }
}
