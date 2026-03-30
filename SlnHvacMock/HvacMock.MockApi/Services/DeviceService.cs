using Amazon.DynamoDBv2.Model;
using HvacMock.MockApi.Models;
using HvacMock.MockApi.Repositories;

namespace HvacMock.MockApi.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _repo;

        public DeviceService(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            var items = await _repo.GetAllAsync();
            return items.Select(MapToDevice).ToList();
        }

        public async Task<Device?> GetByIdAsync(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return null;
            return MapToDevice(item);
        }

        public async Task PatchAsync(string id, string field, string value)
        {
            await _repo.UpdateFieldAsync(id, field, value);
        }

        private Device MapToDevice(Dictionary<string, AttributeValue> item)
        {
            return new Device
            {
                Id = item["deviceId"].S,
                Mode = item["mode"].S,
                Status = item["status"].S,
                Temperature = Convert.ToDouble(item["temperature"].N),
                DeviceModel = item["deviceModel"].S,
                Type = item["type"].S
            };
        }
    }
}