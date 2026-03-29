using Amazon.DynamoDBv2.Model;
using HvacMock.MockApi.Models;
using HvacMock.MockApi.Repositories;

namespace HvacMock.MockApi.Services
{
    public class DeviceService
    {
        private readonly DeviceRepository _repo;

        public DeviceService(DeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            var items = await _repo.GetAllAsync();
            var devices = new List<Device>();

            foreach (var item in items)
            {
                var device = new Device
                {
                    Id = item["deviceId"].S,
                    Mode = item["mode"].S,
                    Status = item["status"].S,
                    Temperature = Convert.ToDouble(item["temperature"].N),
                    DeviceModel = item["deviceModel"].S,
                    Type = item["type"].S
                };

                devices.Add(device);
            }

            return devices;
        }

        public async Task PatchAsync(string id, string field, string value)
        {
            await _repo.UpdateFieldAsync(id, field, value);
        }
    }
}