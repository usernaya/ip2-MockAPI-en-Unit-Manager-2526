using HvacMock.Domain.Devices;

namespace HvacMock.Application.Repositories
{
    public interface IDeviceRepository
    {
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(string deviceId);
        Task SaveAsync(Device device);
        Task DeleteAsync(string deviceId);
        Task CreateTableIfNotExistsAsync();
    }
}
