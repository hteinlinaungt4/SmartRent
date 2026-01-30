using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRent.Data;
using SmartRent.Dto;
using SmartRent.Interface;
using SmartRent.Model;

namespace SmartRent.Controllers
{
    [Authorize] // Protects all methods by default
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            // Token ထဲကနေ User Name ကို ယူခြင်း
            var username = User.Identity?.Name;

            var user = await _context.Users
                .Include(u => u.Properties)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound();

            return Ok(new UserProfileDto(
                user.Id,
                user.Username,
                user.FullName,
                user.AvatarUrl,
                user.IsVerified,
                user.TrustScore,
                user.Properties.Count
            ));
        }


        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<TokenDto>> Register(UserRegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("User already exists.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.CreateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new TokenDto(accessToken, refreshToken));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<TokenDto>> Login(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid username or password.");

            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.CreateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return Ok(new TokenDto(accessToken, refreshToken));
        }

        // CRITICAL CHANGE: Use AllowAnonymous here
        // Because the client calls this AFTER the Access Token has expired.
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenDto>> RefreshToken(TokenRequest request)
        {
            // 1. Find user by the provided Refresh Token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            // 2. Validate token existence and check if it's expired
            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                return Unauthorized("Invalid or expired refresh token. Please login again.");
            }

            // 3. Generate NEW pair (Rotation)
            var newAccessToken = _tokenService.CreateAccessToken(user);
            var newRefreshToken = _tokenService.CreateRefreshToken();

            // 4. Update database
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenDto(newAccessToken, newRefreshToken));
        }
    }
}