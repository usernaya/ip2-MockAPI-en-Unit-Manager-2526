using System.Text.Json;
using HvacMock.Application.Services;
using HvacMock.Application.Tests.Fakes;
using HvacMock.Domain.Devices;

namespace HvacMock.Application.Tests.Services;

public class AdminDeviceServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenIdIsEmpty_ThrowsArgumentException()
    {
        FakeDeviceRepository repository = new();
        AdminDeviceService service = CreateService(repository);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync("", "heating", "Altherma", new List<ManagementPoint>()));

        Assert.Contains("Id", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDeviceAlreadyExists_ThrowsArgumentException()
    {
        FakeDeviceRepository repository = new(BuildDevice("device-1"));
        AdminDeviceService service = CreateService(repository);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync("device-1", "heating", "Altherma", new List<ManagementPoint>()));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDeviceIsNew_SavesAndReturnsDevice()
    {
        FakeDeviceRepository repository = new();
        AdminDeviceService service = CreateService(repository);
        List<ManagementPoint> managementPoints = new() { new ManagementPoint { EmbeddedId = "0" } };

        Device created = await service.CreateAsync("device-9", "heatPump", "Emura", managementPoints);

        Assert.Equal("device-9", created.Id);
        Assert.Equal("heatPump", created.Type);
        Assert.Equal("Emura", created.DeviceModel);
        Assert.Same(created, repository.LastSavedDevice);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeviceDoesNotExist_ReturnsFalse()
    {
        FakeDeviceRepository repository = new();
        AdminDeviceService service = CreateService(repository);

        bool deleted = await service.DeleteAsync("missing");

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeviceExists_RemovesDeviceAndReturnsTrue()
    {
        FakeDeviceRepository repository = new(BuildDevice("device-1"));
        AdminDeviceService service = CreateService(repository);

        bool deleted = await service.DeleteAsync("device-1");

        Assert.True(deleted);
        Assert.Null(await repository.GetByIdAsync("device-1"));
    }

    [Fact]
    public async Task PatchAsync_DelegatesToDeviceService()
    {
        Device device = BuildDevice("device-1");
        device.ManagementPoints[0].OnOffMode = new OnOffModeField
        {
            Settable = true,
            Values = new List<string> { "on", "off" },
            Value = "on"
        };

        FakeDeviceRepository repository = new(device);
        AdminDeviceService service = CreateService(repository);

        Device? updated = await service.PatchAsync(
            "device-1",
            "managementPoints[0].onOffMode.value",
            JsonValue("\"off\""));

        Assert.NotNull(updated);
        Assert.Equal("off", updated.ManagementPoints[0].OnOffMode!.Value);
    }

    private static AdminDeviceService CreateService(FakeDeviceRepository repository)
    {
        DeviceService deviceService = new(repository);
        return new AdminDeviceService(repository, deviceService);
    }

    private static JsonElement JsonValue(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static Device BuildDevice(string id)
    {
        return new Device
        {
            Id = id,
            Type = "heating",
            DeviceModel = "Altherma",
            ManagementPoints = new List<ManagementPoint>
            {
                new()
                {
                    EmbeddedId = "1",
                    ManagementPointType = "climateControl",
                    ManagementPointCategory = "primary"
                }
            }
        };
    }
}
