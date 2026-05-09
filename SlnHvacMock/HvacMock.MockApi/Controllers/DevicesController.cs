using HvacMock.MockApi.Models.Devices;
using HvacMock.MockApi.Models.Requests;
using HvacMock.MockApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HvacMock.MockApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("v1/gateway-devices")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _service;

        public DevicesController(IDeviceService service)
        {
            _service = service;
        }

        // Haalt alle devices op uit DynamoDB
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<Device> devices = await _service.GetAllAsync();
            return Ok(devices);
        }

        // Haalt één device op op basis van deviceId
        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetById(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return BadRequest("deviceId is verplicht");
            }

            try
            {
                Device? device = await _service.GetByIdAsync(deviceId);

                if (device == null)
                {
                    return NotFound($"Device met id '{deviceId}' niet gevonden");
                }

                return Ok(device);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Fout bij ophalen van device: {ex.Message}");
            }
        }

        // Past één veld aan via een dynamisch genest pad binnen de bestaande JSON-structuur.
        // {**field} is een catch-all zodat paden met punten correct worden doorgegeven.
        // Voorbeeld: PATCH /v1/gateway-devices/device-1/managementPoints[0].targetTemperature.value
        [HttpPatch("{deviceId}/{**field}")]
        public async Task<IActionResult> Patch(string deviceId, string field, [FromBody] PatchRequest? request)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return BadRequest("deviceId is verplicht");
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                return BadRequest("field-pad is verplicht. Voorbeeld: managementPoints[1].targetTemperature.value");
            }

            if (request == null)
            {
                return BadRequest("Request body is verplicht. Voorbeeld: { \"value\": 20.5 }");
            }

            try
            {
                Device? updated = await _service.PatchAsync(deviceId, field, request.Value);

                if (updated == null)
                {
                    return NotFound($"Device met id '{deviceId}' niet gevonden");
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Fout bij uitvoeren van PATCH: {ex.Message}");
            }
        }
    }
}
