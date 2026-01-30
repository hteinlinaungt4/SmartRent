namespace SmartRent.Dto
{
    public record UserRegisterDto(string Username, string Password);
    public record UserLoginDto(string Username, string Password);

    // Profile Page မှာ ပြဖို့အတွက် Detail DTO
    public record UserProfileDto(
        Guid Id,
        string Username,
        string? FullName,
        string? AvatarUrl,
        bool IsVerified,
        int TrustScore,
        int PostCount
    );

    public record TokenRequest(string RefreshToken);
    public record TokenDto(string AccessToken, string RefreshToken);
}