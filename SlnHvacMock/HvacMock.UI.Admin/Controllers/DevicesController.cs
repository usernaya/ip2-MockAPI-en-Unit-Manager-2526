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

        // GET /devices/dashboard → statistieken overzicht
        public async Task<IActionResult> Dashboard()
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            string json = await client.GetStringAsync($"{ApiBase}/admin/devices");
            List<Device> devices = JsonSerializer.Deserialize<List<Device>>(json, JsonOptions)
                ?? new List<Device>();

            DashboardViewModel vm = new DashboardViewModel
            {
                TotalDevices       = devices.Count,
                ActiveDevices      = devices.Count(d => d.Status == "on"),
                InactiveDevices    = devices.Count(d => d.Status != "on"),
                AverageTemperature = devices.Any() ? Math.Round(devices.Average(d => d.Temperature), 1) : 0,
                ModeDistribution   = devices
                    .GroupBy(d => d.Mode)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TypeDistribution   = devices
                    .GroupBy(d => d.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Devices            = devices.OrderBy(d => d.Id).ToList()
            };

            return View(vm);
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
        public async Task<IActionResult> Create()
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            await PopulateCreateDefaultsAsync(client);
            return View();
        }

        // POST /devices/create → maak nieuw device aan via Admin API
        // The UI collects only admin-facing fields; the default HVAC structure is generated here.
        [HttpPost]
        public async Task<IActionResult> Create(string id, string type, string deviceModel, string? displayName)
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            id = string.IsNullOrWhiteSpace(id)
                ? await GenerateNextDeviceIdAsync(client)
                : id.Trim();

            Device? existingDevice = await FetchDeviceOrNullAsync(client, id);
            if (existingDevice != null)
            {
                await PopulateCreateDefaultsAsync(client, id, type, deviceModel, displayName);
                ViewBag.Error = $"Device '{id}' already exists. Choose another ID or use the suggested next ID.";
                return View();
            }

            string climateDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? deviceModel
                : displayName.Trim();

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
                        name = new { settable = false, value = climateDisplayName, maxLength = 63 },
                        onOffMode = new
                        {
                            settable = true,
                            values = new[] { "on", "off" },
                            value = "off"
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
                            settable = false,
                            value = new
                            {
                                operationModes = new Dictionary<string, object>
                                {
                                    ["heating"] = new
                                    {
                                        setpoints = new Dictionary<string, object>
                                        {
                                            ["roomTemperature"] = new
                                            {
                                                settable = true,
                                                value = 21.0,
                                                minValue = 12.0,
                                                maxValue = 30.0,
                                                stepValue = 0.5
                                            }
                                        }
                                    },
                                    ["cooling"] = new
                                    {
                                        setpoints = new Dictionary<string, object>
                                        {
                                            ["roomTemperature"] = new
                                            {
                                                settable = true,
                                                value = 24.0,
                                                minValue = 16.0,
                                                maxValue = 32.0,
                                                stepValue = 0.5
                                            }
                                        }
                                    },
                                    ["auto"] = new
                                    {
                                        setpoints = new Dictionary<string, object>
                                        {
                                            ["roomTemperature"] = new
                                            {
                                                settable = true,
                                                value = 22.0,
                                                minValue = 12.0,
                                                maxValue = 32.0,
                                                stepValue = 0.5
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            string body = JsonSerializer.Serialize(newDevice);
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{ApiBase}/admin/devices", content);

            if (!response.IsSuccessStatusCode)
            {
                await PopulateCreateDefaultsAsync(client, id, type, deviceModel, displayName);
                string apiError = await response.Content.ReadAsStringAsync();
                ViewBag.Error = string.IsNullOrWhiteSpace(apiError)
                    ? $"Could not create device. Device '{id}' may already exist."
                    : apiError;
                return View();
            }

            TempData["Success"] = "Device created successfully. You can now configure runtime behavior and setpoints.";
            return RedirectToAction("Details", new { id });
        }

        private async Task PopulateCreateDefaultsAsync(
            HttpClient client,
            string? attemptedId = null,
            string? type = null,
            string? deviceModel = null,
            string? displayName = null)
        {
            ViewBag.SuggestedId = await GenerateNextDeviceIdAsync(client);
            ViewBag.AttemptedId = attemptedId;
            ViewBag.SelectedType = string.IsNullOrWhiteSpace(type) ? "airConditioner" : type;
            ViewBag.DeviceModel = deviceModel;
            ViewBag.DisplayName = displayName;
        }

        private async Task<string> GenerateNextDeviceIdAsync(HttpClient client)
        {
            string json = await client.GetStringAsync($"{ApiBase}/admin/devices");
            List<Device> devices = JsonSerializer.Deserialize<List<Device>>(json, JsonOptions)
                ?? new List<Device>();

            HashSet<int> usedNumbers = devices
                .Select(d => TryParseDeviceNumber(d.Id))
                .Where(number => number.HasValue && number.Value > 0)
                .Select(number => number!.Value)
                .ToHashSet();

            int next = 1;
            while (usedNumbers.Contains(next))
            {
                next++;
            }

            return $"device-{next}";
        }

        private static int? TryParseDeviceNumber(string? id)
        {
            const string prefix = "device-";
            if (string.IsNullOrWhiteSpace(id) ||
                !id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(id[prefix.Length..], out int number)
                ? number
                : null;
        }

        private async Task<Device?> FetchDeviceOrNullAsync(HttpClient client, string id)
        {
            HttpResponseMessage response = await client.GetAsync($"{ApiBase}/admin/devices/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Device>(json, JsonOptions);
        }

        // POST /devices/patch → pas één veld aan via Admin API PATCH
        [HttpPost]
        public async Task<IActionResult> SetUserReadonly(string id, bool isReadonly)
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            Device? device = await FetchDeviceOrNullAsync(client, id);
            if (device == null)
            {
                return NotFound();
            }

            int primaryIndex = device.ManagementPoints.FindIndex(m =>
                m.ManagementPointType == "climateControl" ||
                m.ManagementPointCategory == "primary");

            if (primaryIndex < 0)
            {
                TempData["Error"] = "No climate control management point found.";
                return RedirectToAction("Details", new { id });
            }

            ManagementPoint primary = device.ManagementPoints[primaryIndex];
            bool settable = !isReadonly;
            List<string> paths = new();

            if (primary.OnOffMode != null)
            {
                paths.Add($"managementPoints[{primaryIndex}].onOffMode.settable");
            }

            if (primary.OperationMode != null)
            {
                paths.Add($"managementPoints[{primaryIndex}].operationMode.settable");
            }

            if (primary.TargetTemperature != null)
            {
                paths.Add($"managementPoints[{primaryIndex}].targetTemperature.settable");
            }

            if (primary.TemperatureControl?.Value?.OperationModes != null)
            {
                foreach (var mode in primary.TemperatureControl.Value.OperationModes)
                {
                    foreach (var setpoint in mode.Value.Setpoints)
                    {
                        paths.Add($"managementPoints[{primaryIndex}].temperatureControl.value.operationModes.{mode.Key}.setpoints.{setpoint.Key}.settable");
                    }
                }
            }

            foreach (string path in paths)
            {
                HttpResponseMessage response = await PatchDeviceFieldAsync(client, id, path, settable);
                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = string.IsNullOrWhiteSpace(error)
                        ? "Could not update user editability."
                        : error;
                    return RedirectToAction("Details", new { id });
                }
            }

            TempData["Success"] = isReadonly
                ? "Device is now read-only for users."
                : "Device is now editable for users.";

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Patch(string id, string field, string value)
        {
            HttpClient? client = GetAuthenticatedClient();
            if (client == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(field))
            {
                TempData["Error"] = "Missing device or field path.";
                return RedirectToAction("Details", new { id });
            }

            // Keep old shortcut names working, but allow exact JSON paths for advanced admin fields.
            string path = field switch
            {
                "targetTemperature" => "managementPoints[1].targetTemperature.value",
                "operationMode"     => "managementPoints[1].operationMode.value",
                "onOffMode"         => "managementPoints[1].onOffMode.value",
                _                   => field
            };

            object parsedValue = ParsePatchValue(value);
            HttpResponseMessage response = await PatchDeviceFieldAsync(client, id, path, parsedValue);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Device configuration updated.";
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(error)
                    ? "The Admin API rejected this configuration update."
                    : error;
            }

            return RedirectToAction("Details", new { id });
        }

        private async Task<HttpResponseMessage> PatchDeviceFieldAsync(
            HttpClient client,
            string id,
            string path,
            object value)
        {
            string body = JsonSerializer.Serialize(new { path, value });
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

            return await client.PatchAsync($"{ApiBase}/admin/devices/{id}", content);
        }

        private static object ParsePatchValue(string? value)
        {
            if (bool.TryParse(value, out bool boolean))
            {
                return boolean;
            }

            if (double.TryParse(value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double number))
            {
                return number;
            }

            return value ?? string.Empty;
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
