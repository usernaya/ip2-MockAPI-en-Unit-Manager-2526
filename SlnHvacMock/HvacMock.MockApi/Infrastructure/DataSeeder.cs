using HvacMock.MockApi.Models.Devices;
using HvacMock.MockApi.Repositories;
using System.Text.Json;

namespace HvacMock.MockApi.Infrastructure
{
    public class DataSeeder
    {
        private readonly IDeviceRepository _repo;
        private readonly IWebHostEnvironment _environment;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public DataSeeder(IDeviceRepository repo, IWebHostEnvironment environment)
        {
            _repo = repo;
            _environment = environment;
        }

        public async Task SeedAsync()
        {
            await _repo.CreateTableIfNotExistsAsync();

            List<Device> existing = await _repo.GetAllAsync();
            if (existing.Count > 0)
            {
                return;
            }

            List<Device> seedDevices = LoadSeedDevices();

            foreach (Device device in seedDevices)
            {
                await _repo.SaveAsync(device);
            }
        }

        private List<Device> LoadSeedDevices()
        {
            string seedPath = Path.Combine(_environment.ContentRootPath, "Infrastructure", "seed-devices.json");
            string json = File.ReadAllText(seedPath);

            return JsonSerializer.Deserialize<List<Device>>(json, JsonOptions) ?? new List<Device>();
        }
    }
}
