using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HvacMock.AdminApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        // POST /auth/token
        // Body: { "username": "admin", "password": "admin123" }
        [HttpPost("token")]
        public IActionResult GetToken([FromBody] LoginRequest request)
        {
            // Hardcoded credentials voor demo-doeleinden
            if (request.Username != "admin" || request.Password != "admin123")
            {
                return Unauthorized(new { message = "Ongeldige gebruikersnaam of wachtwoord." });
            }

            string token = GenerateJwtToken();
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:ExpiresInMinutes"]!));

            return Ok(new
            {
                token,
                expiresAt
            });
        }

        private string GenerateJwtToken()
        {
            string secret = _config["Jwt:Secret"]!;
            string issuer = _config["Jwt:Issuer"]!;
            string audience = _config["Jwt:Audience"]!;
            int expiresInMinutes = int.Parse(_config["Jwt:ExpiresInMinutes"]!);

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            List<Claim> claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, "admin"),
                new Claim(JwtRegisteredClaimNames.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin")
            };

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
