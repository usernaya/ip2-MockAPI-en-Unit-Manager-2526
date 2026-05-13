using HvacMock.Domain.Devices;
using System.Text.Json;

namespace HvacMock.Application.Services
{
    public interface IAdminDeviceService
    {
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(string id);
        Task<Device> CreateAsync(string id, string type, string deviceModel, List<ManagementPoint> managementPoints);
        Task<Device?> PatchAsync(string id, string path, JsonElement value);
        Task<bool> DeleteAsync(string id);
    }
}
