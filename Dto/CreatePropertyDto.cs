public class CreatePropertyDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Price { get; set; }
    public string Township { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public int Beds { get; set; }
    public int Baths { get; set; }
    public int Sqft { get; set; }

    // URL string အစား File ကို တိုက်ရိုက်ယူပါမယ်
    public List<IFormFile>? Images { get; set; }
}