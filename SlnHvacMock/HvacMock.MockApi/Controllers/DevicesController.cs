using HvacMock.MockApi.Models;
using HvacMock.MockApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HvacMock.MockApi.Controllers
{
    [ApiController]
    [Route("v1/gateway-devices")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _service;

        public DevicesController(IDeviceService service)
        {
            _service = service;
        }

        // haalt alle devices op
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Device> devices = await _service.GetAllAsync();

            return Ok(devices);
        }

        // haalt één device op op basis van id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Invalid id");
            }

            Device device = await _service.GetByIdAsync(id);

            if (device == null)
            {
                return NotFound();
            }

            return Ok(device);
        }

        // past één veld aan van een device
        [HttpPatch("{id}/{field}")]
        public async Task<IActionResult> Patch(string id, string field, [FromBody] PatchRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Invalid id");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Value))
            {
                return BadRequest("Invalid value");
            }

            if (field != "status" && field != "mode" && field != "temperature")
            {
                return BadRequest("Invalid field");
            }

            await _service.PatchAsync(id, field, request.Value);

            return Ok();
        }
    }
}