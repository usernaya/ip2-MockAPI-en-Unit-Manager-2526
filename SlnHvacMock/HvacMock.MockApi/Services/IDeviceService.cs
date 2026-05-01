using HvacMock.MockApi.Models;
using System.Text.Json;

namespace HvacMock.MockApi.Services
{
    public interface IDeviceService
    {
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(string deviceId);

        // Geeft null als device niet bestaat, gooit ArgumentException als pad niet bestaat
        Task<Device?> PatchAsync(string deviceId, string path, JsonElement newValue);
    }
}
