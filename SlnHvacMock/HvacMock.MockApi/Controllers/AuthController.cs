using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace HvacMock.MockApi.Controllers
{
    [ApiController]
    [Route("v1/oidc")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        // Geeft een JWT terug bij correcte client_id en client_secret
        // Zelfde formaat als de echte Daikin API (token_response.json van de klant)
        [HttpPost("token")]
        public IActionResult Token(
            [FromForm(Name = "grant_type")] string grantType,
            [FromForm(Name = "client_id")] string clientId,
            [FromForm(Name = "client_secret")] string clientSecret)
        {
            if (grantType != "client_credentials"
                || clientId != _config["Auth:ClientId"]
                || clientSecret != _config["Auth:ClientSecret"])
            {
                return Unauthorized(new { error = "invalid_client", error_description = "Ongeldige client_id of client_secret" });
            }

            string token = GenerateJwtToken();
            int expiresIn = int.Parse(_config["Jwt:ExpiresInSeconds"] ?? "3600");

            return Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = expiresIn
            });
        }

        private string GenerateJwtToken()
        {
            string secretKey = _config["Jwt:SecretKey"]!;
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddSeconds(int.Parse(_config["Jwt:ExpiresInSeconds"] ?? "3600")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
