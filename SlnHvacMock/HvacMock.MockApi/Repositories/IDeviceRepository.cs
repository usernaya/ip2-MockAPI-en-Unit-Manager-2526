using HvacMock.MockApi.Models.Devices;

namespace HvacMock.MockApi.Repositories
{
    public interface IDeviceRepository
    {
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(string deviceId);
        Task SaveAsync(Device device);
        Task CreateTableIfNotExistsAsync();
    }
}
