using HvacMock.Application.Repositories;
using HvacMock.Domain.Devices;
using System.Text.Json;

namespace HvacMock.Infrastructure.Seeding
{
    public class DataSeeder
    {
        private readonly IDeviceRepository _repo;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public DataSeeder(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task SeedAsync(string seedPath)
        {
            await _repo.CreateTableIfNotExistsAsync();

            List<Device> existing = await _repo.GetAllAsync();
            if (existing.Count > 0)
            {
                return;
            }

            List<Device> seedDevices = LoadSeedDevices(seedPath);

            foreach (Device device in seedDevices)
            {
                await _repo.SaveAsync(device);
            }
        }

        private static List<Device> LoadSeedDevices(string seedPath)
        {
            string json = File.ReadAllText(seedPath);

            return JsonSerializer.Deserialize<List<Device>>(json, JsonOptions) ?? new List<Device>();
        }
    }
}
