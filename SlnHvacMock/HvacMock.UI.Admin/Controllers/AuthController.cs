using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace HvacMock.UI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _config;

        // Sleutel om het token in de sessie op te slaan
        public const string TokenSessionKey = "jwt_token";

        public AuthController(IHttpClientFactory factory, IConfiguration config)
        {
            _factory = factory;
            _config = config;
        }

        // GET /auth/login → toon het loginformulier
        [HttpGet]
        public IActionResult Login()
        {
            // Als er al een token is, ga dan direct naar de devices lijst
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(TokenSessionKey)))
            {
                return RedirectToAction("Index", "Devices");
            }

            return View();
        }

        // POST /auth/login → stuur credentials naar Admin API, sla token op
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            string apiBase = _config["AdminApi:BaseUrl"]!;
            HttpClient client = _factory.CreateClient();

            string body = JsonSerializer.Serialize(new { username, password });
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{apiBase}/auth/token", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Ongeldige gebruikersnaam of wachtwoord.";
                return View();
            }

            string json = await response.Content.ReadAsStringAsync();
            JsonDocument doc = JsonDocument.Parse(json);
            string token = doc.RootElement.GetProperty("token").GetString()!;

            // Token opslaan in sessie
            HttpContext.Session.SetString(TokenSessionKey, token);

            return RedirectToAction("Index", "Devices");
        }

        // GET /auth/logout → token verwijderen en terug naar login
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(TokenSessionKey);
            return RedirectToAction("Login");
        }
    }
}
