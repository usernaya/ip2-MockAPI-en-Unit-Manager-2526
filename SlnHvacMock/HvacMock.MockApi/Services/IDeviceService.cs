using HvacMock.MockApi.Models;

namespace HvacMock.MockApi.Services
{
    public interface IDeviceService
    {
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(string id);
        Task PatchAsync(string id, string field, string value);
    }
}