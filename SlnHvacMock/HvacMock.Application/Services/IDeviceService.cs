using HvacMock.Domain.Devices;
using System.Text.Json;

namespace HvacMock.Application.Services
{
    public interface IDeviceService
    {
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(string deviceId);
        Task<Device?> PatchAsync(string deviceId, string path, JsonElement newValue);
    }
}
