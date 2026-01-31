namespace SmartRent.Dto
{
    // Category အသစ်ဆောက်ဖို့
    public record CreateCategoryDto(string Name, string? IconName);

    public record UpdateCategoryDto(string Name, string? IconName);



    // Category ပြန်ထုတ်ပြဖို့ (Response)
    public record CategoryResponseDto(int Id, string Name, string IconName);
}