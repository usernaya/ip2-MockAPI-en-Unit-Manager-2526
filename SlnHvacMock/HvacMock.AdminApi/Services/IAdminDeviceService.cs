using HvacMock.AdminApi.DTOs;
using HvacMock.AdminApi.Models;

namespace HvacMock.AdminApi.Services
{
    public interface IAdminDeviceService
    {
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(string id);
        Task<Device> CreateAsync(CreateDeviceRequest request);
        Task<Device?> PatchAsync(string id, PatchRequest req);
        Task<bool> DeleteAsync(string id);
    }
}
