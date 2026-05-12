using HvacMock.AdminApi.DTOs;
using HvacMock.AdminApi.Models;
using HvacMock.AdminApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HvacMock.AdminApi.Controllers
{
    [ApiController]
    [Route("admin/devices")]
    [Authorize] // Alle endpoints vereisen een geldig JWT-token
    public class AdminDevicesController : ControllerBase
    {
        private readonly IAdminDeviceService _service;

        public AdminDevicesController(IAdminDeviceService service)
        {
            _service = service;
        }

        // GET /admin/devices
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<Device> devices = await _service.GetAllAsync();
            return Ok(devices);
        }

        // GET /admin/devices/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            Device? device = await _service.GetByIdAsync(id);
            if (device == null)
            {
                return NotFound(new { message = $"Device '{id}' niet gevonden." });
            }

            return Ok(device);
        }

        // POST /admin/devices
        // Body: volledig device-object
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDeviceRequest request)
        {
            try
            {
                Device device = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH /admin/devices/{id}
        // Body: { "path": "managementPoints[0].targetTemperature.value", "value": 22 }
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] PatchRequest request)
        {
            try
            {
                Device? updatedDevice = await _service.PatchAsync(id, request);
                if (updatedDevice == null)
                {
                    return NotFound(new { message = $"Device '{id}' niet gevonden." });
                }

                return Ok(updatedDevice);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE /admin/devices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            bool deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Device '{id}' niet gevonden." });
            }

            return NoContent();
        }
    }
}
