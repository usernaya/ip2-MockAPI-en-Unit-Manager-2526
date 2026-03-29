using HvacMock.MockApi.Models;
using HvacMock.MockApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HvacMock.MockApi.Controllers
{
    [ApiController]
    [Route("v1/gateway-devices")]
    public class DevicesController : ControllerBase
    {
        private readonly DeviceService _service;

        public DevicesController(DeviceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpPatch("{id}/{field}")]
        public async Task<IActionResult> Patch(string id, string field, [FromBody] PatchRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Value))
                return BadRequest();

            if (field != "status" && field != "mode" && field != "temperature")
                return BadRequest("Invalid field");

            await _service.PatchAsync(id, field, request.Value);
            return Ok();
        }
    }
}