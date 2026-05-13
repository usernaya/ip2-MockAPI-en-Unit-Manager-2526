using HvacMock.Application.Repositories;
using HvacMock.Domain.Devices;

namespace HvacMock.Application.Tests.Fakes;

internal sealed class FakeDeviceRepository : IDeviceRepository
{
    private readonly Dictionary<string, Device> _devices;

    public FakeDeviceRepository(params Device[] devices)
    {
        _devices = devices.ToDictionary(device => device.Id);
    }

    public Device? LastSavedDevice { get; private set; }
    public bool TableCreationRequested { get; private set; }

    public Task<List<Device>> GetAllAsync()
    {
        return Task.FromResult(_devices.Values.ToList());
    }

    public Task<Device?> GetByIdAsync(string deviceId)
    {
        _devices.TryGetValue(deviceId, out Device? device);
        return Task.FromResult(device);
    }

    public Task SaveAsync(Device device)
    {
        _devices[device.Id] = device;
        LastSavedDevice = device;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string deviceId)
    {
        _devices.Remove(deviceId);
        return Task.CompletedTask;
    }

    public Task CreateTableIfNotExistsAsync()
    {
        TableCreationRequested = true;
        return Task.CompletedTask;
    }
}
