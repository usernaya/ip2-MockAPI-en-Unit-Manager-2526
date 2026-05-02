using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HvacMock.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HvacMock.UI.Controllers
{
    public class DevicesController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _config;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public DevicesController(IHttpClientFactory factory, IConfiguration config)
        {
            _factory = factory;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            HttpClient client = _factory.CreateClient();
            await AddTokenAsync(client);

            string json = await client.GetStringAsync($"{ApiBase}/gateway-devices");
            List<Device> devices = JsonSerializer.Deserialize<List<Device>>(json, JsonOptions) ?? new();
            devices = devices.OrderBy(d => d.Id).ToList();

            return View(devices);
        }

        public async Task<IActionResult> Details(string id)
        {
            HttpClient client = _factory.CreateClient();
            await AddTokenAsync(client);

            string json = await client.GetStringAsync($"{ApiBase}/gateway-devices/{id}");
            Device device = JsonSerializer.Deserialize<Device>(json, JsonOptions)!;

            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Patch(string id, string field, string value)
        {
            HttpClient client = _factory.CreateClient();
            await AddTokenAsync(client);

            // Send numeric values as JSON numbers so the API can validate ranges.
            object typedValue = TryParseNumber(value, out double num) ? num : value;

            string body = JsonSerializer.Serialize(new { value = typedValue });
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PatchAsync($"{ApiBase}/gateway-devices/{id}/{field}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Instelling bijgewerkt.";
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(error)
                    ? "Update mislukt. Controleer de waarde en probeer opnieuw."
                    : error.Trim('"');
            }

            return RedirectToAction("Details", new { id });
        }

        private static bool TryParseNumber(string value, out double number)
        {
            string normalized = value.Contains('.') ? value : value.Replace(',', '.');

            return double.TryParse(
                normalized,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out number);
        }

        private async Task AddTokenAsync(HttpClient client)
        {
            string tokenEndpoint = $"{ApiBase}/oidc/token";

            FormUrlEncodedContent form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _config["MockApi:ClientId"]!),
                new KeyValuePair<string, string>("client_secret", _config["MockApi:ClientSecret"]!)
            });

            HttpResponseMessage response = await client.PostAsync(tokenEndpoint, form);
            string json = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            JsonDocument doc = JsonDocument.Parse(json);
            string token = doc.RootElement.GetProperty("access_token").GetString()!;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private string ApiBase => _config["MockApi:BaseUrl"]!;
    }
}
