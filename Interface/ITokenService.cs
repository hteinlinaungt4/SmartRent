using SmartRent.Model;

namespace SmartRent.Interface
{
    public interface ITokenService
    {
        string CreateAccessToken(User user);
        string CreateRefreshToken();
    }
}
