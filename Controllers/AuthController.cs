using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRent.Data;
using SmartRent.Dto;
using SmartRent.Interface;
using SmartRent.Model;


namespace SmartRent.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;


      
        public AuthController(DataContext context,ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }



        [HttpGet]
        public IActionResult GetData()
        {
            return Ok("This data is only visible if you have a valid Access Token!");
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

            // Generate tokens immediately
            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.CreateRefreshToken();

            // Save user with Refresh Token
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
                return BadRequest("Wrong username or password.");

            // Generate tokens
            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.CreateRefreshToken();

            // Save refresh token to PostgreSQL
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenDto(accessToken, refreshToken));
        }


        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenDto>> RefreshToken(TokenDto request)
        {
            // 1. Find the user by the Refresh Token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            // 2. Validate existence and expiration
            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }

            // 3. Generate a NEW pair (Refresh Token Rotation for security)
            var newAccessToken = _tokenService.CreateAccessToken(user);
            var newRefreshToken = _tokenService.CreateRefreshToken();

            // 4. Update the database with the new refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenDto(newAccessToken, newRefreshToken));
        }
    }
}
