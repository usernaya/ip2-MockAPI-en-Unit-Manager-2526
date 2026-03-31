using System.Text;
using System.Text.Json;
using HvacMock.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HvacMock.UI.Controllers
{
    public class DevicesController : Controller
    {
        private readonly IHttpClientFactory _factory;

        // aanpassen als poort verandert, kan ook in appsettings.json maar we doen het later wel goed
        private const string ApiBase = "https://localhost:7049/v1";

        public DevicesController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public async Task<IActionResult> Index()
        {
            HttpClient client = _factory.CreateClient();

            string json = await client.GetStringAsync($"{ApiBase}/gateway-devices");

            List<Device> devices = JsonSerializer.Deserialize<List<Device>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(devices);
        }

        public async Task<IActionResult> Details(string id)
        {
            HttpClient client = _factory.CreateClient();

            string json = await client.GetStringAsync($"{ApiBase}/gateway-devices/{id}");

            Device device = JsonSerializer.Deserialize<Device>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Patch(string id, string field, string value)
        {
            HttpClient client = _factory.CreateClient();

            string body = JsonSerializer.Serialize(new { value });
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

            await client.PatchAsync($"{ApiBase}/gateway-devices/{id}/{field}", content);

            return RedirectToAction("Details", new { id });
        }
    }
}