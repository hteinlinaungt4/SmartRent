namespace SmartRent.Dto
{
    public record UserRegisterDto(string Username, string Password);
    public record UserLoginDto(string Username, string Password);

    public record TokenRequest(string RefreshToken);
    public record TokenDto(string AccessToken, string RefreshToken);
}
