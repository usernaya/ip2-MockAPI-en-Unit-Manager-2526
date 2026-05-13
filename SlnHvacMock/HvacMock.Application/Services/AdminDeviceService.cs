using HvacMock.Application.Repositories;
using HvacMock.Domain.Devices;
using System.Text.Json;

namespace HvacMock.Application.Services
{
    public class AdminDeviceService : IAdminDeviceService
    {
        private readonly IDeviceRepository _repo;
        private readonly IDeviceService _deviceService;

        public AdminDeviceService(IDeviceRepository repo, IDeviceService deviceService)
        {
            _repo = repo;
            _deviceService = deviceService;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Device?> GetByIdAsync(string id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<Device> CreateAsync(string id, string type, string deviceModel, List<ManagementPoint> managementPoints)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Een device moet een Id hebben.");
            }

            Device? existingDevice = await _repo.GetByIdAsync(id);
            if (existingDevice != null)
            {
                throw new ArgumentException($"Device '{id}' already exists. Choose another ID.");
            }

            Device device = new Device
            {
                Id = id,
                Type = type,
                DeviceModel = deviceModel,
                ManagementPoints = managementPoints
            };

            await _repo.SaveAsync(device);
            return device;
        }

        public async Task<Device?> PatchAsync(string id, string path, JsonElement value)
        {
            return await _deviceService.PatchAsync(id, path, value);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            Device? device = await _repo.GetByIdAsync(id);
            if (device == null)
            {
                return false;
            }

            await _repo.DeleteAsync(id);
            return true;
        }
    }
}
