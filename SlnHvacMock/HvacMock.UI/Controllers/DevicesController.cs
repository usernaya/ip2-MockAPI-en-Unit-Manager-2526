using HvacMock.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

        // Haalt de Admin API base URL op uit appsettings.json
        private string ApiBase => _config["AdminApi:BaseUrl"]!;

        // Maakt een HttpClient aan met het JWT-token uit de sessie.
        // Geeft null terug als er geen token is → controller redirectt naar login.
        private HttpClient? GetAuthenticatedClient()
        {
            string? token = HttpContext.Session.GetString(AuthController.TokenSessionKey);
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            HttpClient client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // GET /devices → lijst van alle devices
        public async Task<IActionResult> Index()
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            string json = await client.GetStringAsync($"{ApiBase}/admin/devices");
            List<Device> devices = JsonSerializer.Deserialize<List<Device>>(json, JsonOptions)
                ?? new List<Device>();

            devices = devices.OrderBy(d => d.Id).ToList();
            return View(devices);
        }

        // GET /devices/details/{id} → detail van één device
        public async Task<IActionResult> Details(string id)
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            HttpResponseMessage response = await client.GetAsync($"{ApiBase}/admin/devices/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            string json = await response.Content.ReadAsStringAsync();
            Device device = JsonSerializer.Deserialize<Device>(json, JsonOptions)!;

            return View(device);
        }

        // GET /devices/create → toon het aanmaakformulier
        [HttpGet]
        public IActionResult Create()
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            return View();
        }

        // POST /devices/create → maak nieuw device aan via Admin API
        // Gebruiker vult enkel Id, Type en DeviceModel in.
        // De controller bouwt de volledige managementPoints-structuur op.
        [HttpPost]
        public async Task<IActionResult> Create(string id, string type, string deviceModel)
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            var newDevice = new
            {
                id,
                type,
                deviceModel,
                managementPoints = new object[]
                {
                    new
                    {
                        embeddedId = "0",
                        managementPointType = "gateway",
                        managementPointCategory = "secondary",
                        name = new { settable = false, value = "Gateway", maxLength = 63 }
                    },
                    new
                    {
                        embeddedId = "1",
                        managementPointType = "climateControl",
                        managementPointCategory = "primary",
                        name = new { settable = false, value = deviceModel, maxLength = 63 },
                        onOffMode = new
                        {
                            settable = true,
                            values = new[] { "on", "off" },
                            value = "on"
                        },
                        operationMode = new
                        {
                            settable = true,
                            values = new[] { "heating", "cooling", "auto" },
                            value = "heating"
                        },
                        targetTemperature = new
                        {
                            settable = true,
                            value = 21.0,
                            minValue = 12.0,
                            maxValue = 30.0,
                            stepValue = 0.5
                        },
                        temperatureControl = new
                        {
                            @ref = "#temperatureControl",
                            settable = false
                        }
                    }
                }
            };

            string body = JsonSerializer.Serialize(newDevice);
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{ApiBase}/admin/devices", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Kon device niet aanmaken. Controleer of het ID al bestaat.";
                return View();
            }

            return RedirectToAction("Index");
        }

        // POST /devices/patch → pas één veld aan via Admin API PATCH
        [HttpPost]
        public async Task<IActionResult> Patch(string id, string field, string value)
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            // Zet de veldnaam om naar het volledige pad (managementPoints[1] = climateControl)
            string path = field switch
            {
                "targetTemperature" => "managementPoints[1].targetTemperature.value",
                "operationMode"     => "managementPoints[1].operationMode.value",
                "onOffMode"         => "managementPoints[1].onOffMode.value",
                _                   => "managementPoints[1].targetTemperature.value"
            };

            // Getal of string: temperatuur is een getal, mode is een string
            object parsedValue = double.TryParse(value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double number)
                ? (object)number
                : value;

            string body = JsonSerializer.Serialize(new { path, value = parsedValue });
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

            await client.PatchAsync($"{ApiBase}/admin/devices/{id}", content);

            return RedirectToAction("Details", new { id });
        }

        // POST /devices/delete → verwijder een device
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            await client.DeleteAsync($"{ApiBase}/admin/devices/{id}");

            return RedirectToAction("Index");
        }
    }
}
