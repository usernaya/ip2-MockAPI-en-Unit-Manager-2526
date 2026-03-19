using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;
using HvacMockApi.Models;

namespace HvacMockApi.Controllers
{
    [ApiController]
    [Route("api/devices")]
    public class DevicesController : ControllerBase
    {
        private readonly IAmazonDynamoDB _dynamoDb;

        public DevicesController(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        // GET: api/devices
        [HttpGet]
        public async Task<IActionResult> GetAllDevices()
        {
            ScanRequest request = new ScanRequest
            {
                TableName = "Devices"
            };

            ScanResponse response = await _dynamoDb.ScanAsync(request);

            List<Device> devices = new List<Device>();

            foreach (var item in response.Items)
            {
                Device device = new Device
                {
                    Id = item["deviceId"].S,
                    Type = "heating",
                    DeviceModel = "Altherma",
                    IsCloudConnectionUp = new CloudConnection
                    {
                        Value = true
                    },
                    ManagementPoints = new List<ManagementPoint>
            {
                new ManagementPoint
                {
                    EmbeddedId = "1",
                    ManagementPointType = "climateControl",
                    Name = new Name { Value = "Living room" },
                    OnOffMode = new Mode { Value = item["status"].S },
                    OperationMode = new Mode { Value = item["mode"].S },
                    TargetTemperature = new Temperature
                    {
                        Value = int.Parse(item["temperature"].N)
                    },
                    SensoryData = new SensoryData
                    {
                        RoomTemperature = new Temperature
                        {
                            Value = int.Parse(item["temperature"].N)
                        }
                    }
                }
            }
                };

                devices.Add(device);
            }

            return Ok(devices);
        }

        // GET: api/devices/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceById(string id)
        {
            ScanRequest request = new ScanRequest
            {
                TableName = "Devices"
            };

            ScanResponse response = await _dynamoDb.ScanAsync(request);

            foreach (var item in response.Items)
            {
                if (item["deviceId"].S == id)
                {
                    Device device = new Device
                    {
                        Id = item["deviceId"].S,
                        Type = "heating",
                        DeviceModel = "Altherma",
                        IsCloudConnectionUp = new CloudConnection
                        {
                            Value = true
                        },
                        ManagementPoints = new List<ManagementPoint>
                {
                    new ManagementPoint
                    {
                        EmbeddedId = "1",
                        ManagementPointType = "climateControl",
                        Name = new Name { Value = "Living room" },
                        OnOffMode = new Mode { Value = item["status"].S },
                        OperationMode = new Mode { Value = item["mode"].S },
                        TargetTemperature = new Temperature
                        {
                            Value = int.Parse(item["temperature"].N)
                        },
                        SensoryData = new SensoryData
                        {
                            RoomTemperature = new Temperature
                            {
                                Value = int.Parse(item["temperature"].N)
                            }
                        }
                    }
                }
                    };

                    return Ok(device);
                }
            }

            return NotFound(new { message = "Device niet gevonden" });
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("API werkt!");
        }
    }
}