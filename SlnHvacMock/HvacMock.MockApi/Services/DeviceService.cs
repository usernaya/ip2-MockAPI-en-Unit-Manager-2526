using HvacMock.MockApi.Models.Devices;
using HvacMock.MockApi.Repositories;
using HvacMock.MockApi.Services.Patching;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HvacMock.MockApi.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _repo;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public DeviceService(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Device?> GetByIdAsync(string deviceId)
        {
            return await _repo.GetByIdAsync(deviceId);
        }

        public async Task<Device?> PatchAsync(string deviceId, string path, JsonElement newValue)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Het pad mag niet leeg zijn. Voorbeeld: managementPoints[1].targetTemperature.value");
            }

            Device? device = await _repo.GetByIdAsync(deviceId);

            if (device == null)
            {
                return null;
            }

            string json = JsonSerializer.Serialize(device, JsonOptions);
            JsonNode root = JsonNode.Parse(json)!;

            (JsonNode parentNode, string lastSegment) = DevicePatchNavigator.NavigateToParent(root, path);

            PatchFieldValidator.ValidateAgainstConstraints(parentNode, lastSegment, newValue);

            parentNode[lastSegment] = JsonNode.Parse(newValue.GetRawText());

            string updatedJson = root.ToJsonString();
            Device updatedDevice = JsonSerializer.Deserialize<Device>(updatedJson, JsonOptions)!;

            OperationModeTemperatureUpdater.Apply(updatedDevice, path, newValue);

            await _repo.SaveAsync(updatedDevice);

            return updatedDevice;
        }
    }
}
