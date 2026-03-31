using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Travella.API.Models;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register/traveler")]
        public async Task<IActionResult> RegisterTraveler([FromBody] RegisterTravelerRequest request)
        {
            var user = await _authService.RegisterTravelerAsync(request);
            var expiresAtUtc = DateTime.UtcNow.AddDays(1);
            var token = GenerateJwtToken(user, expiresAtUtc);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role.ToLowerInvariant(),
                CompanyId = user.CompanyId,
                ExpiresAtUtc = expiresAtUtc
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.LoginAsync(request);
            if (user == null)
                return Unauthorized("Invalid login credentials.");

            var expiresAtUtc = DateTime.UtcNow.AddDays(1);
            var token = GenerateJwtToken(user, expiresAtUtc);

            var response = new AuthResponse
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.ToLowerInvariant(),
                UserId = user.UserId,
                CompanyId = user.CompanyId,
                ExpiresAtUtc = expiresAtUtc
            };

            return Ok(response);
        }

        private string GenerateJwtToken(Travella.Application.DTOs.AuthUserDto user, DateTime expiresAtUtc)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("JWT key is missing.");
            var issuer = jwtSection["Issuer"] ?? "Travella.API";
            var audience = jwtSection["Audience"] ?? "Travella.Client";

            var claims = new[]
            {
                new Claim("userId", user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToUpperInvariant()),
                new Claim("companyId", user.CompanyId?.ToString() ?? string.Empty)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}
