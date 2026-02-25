using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;
using HvacMockApi.Models;

namespace HvacMockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IAmazonDynamoDB _dynamoDb;

        public TestController(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        [HttpGet("dynamodb")]
        public async Task<IActionResult> CheckDynamoDb()
        {
            var response = await _dynamoDb.ListTablesAsync();

            return Ok(new
            {
                status = "connected",
                tableCount = response.TableNames.Count,
                tables = response.TableNames
            });
        }

        [HttpGet("devices")]
        public async Task<IActionResult> GetAllDevices()
        {
            var request = new ScanRequest
            {
                TableName = "Devices"
            };

            var response = await _dynamoDb.ScanAsync(request);

            var devices = response.Items.Select(item => new Device
            {
                DeviceId = item["deviceId"].S,
                Temperature = int.Parse(item["temperature"].N),
                Mode = item["mode"].S,
                Status = item["status"].S
            }).ToList();

            return Ok(devices);
        }
    }
}