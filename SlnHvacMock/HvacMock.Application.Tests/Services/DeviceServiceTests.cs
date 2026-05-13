using System.Text.Json;
using HvacMock.Application.Services;
using HvacMock.Application.Tests.Fakes;
using HvacMock.Domain.Devices;

namespace HvacMock.Application.Tests.Services;

public class DeviceServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsDevicesFromRepository()
    {
        FakeDeviceRepository repository = new(BuildDevice("device-1"), BuildDevice("device-2"));
        DeviceService service = new(repository);

        List<Device> devices = await service.GetAllAsync();

        Assert.Equal(2, devices.Count);
        Assert.Contains(devices, device => device.Id == "device-1");
        Assert.Contains(devices, device => device.Id == "device-2");
    }

    [Fact]
    public async Task PatchAsync_WithAllowedOnOffValue_UpdatesAndSavesDevice()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        Device? updated = await service.PatchAsync(
            "device-1",
            "managementPoints[0].onOffMode.value",
            JsonValue("\"off\""));

        Assert.NotNull(updated);
        Assert.Equal("off", updated.ManagementPoints[0].OnOffMode!.Value);
        Assert.Same(updated, repository.LastSavedDevice);
    }

    [Fact]
    public async Task PatchAsync_WhenDeviceDoesNotExist_ReturnsNullAndDoesNotSave()
    {
        FakeDeviceRepository repository = new();
        DeviceService service = new(repository);

        Device? updated = await service.PatchAsync(
            "missing",
            "managementPoints[0].onOffMode.value",
            JsonValue("\"off\""));

        Assert.Null(updated);
        Assert.Null(repository.LastSavedDevice);
    }

    [Fact]
    public async Task PatchAsync_WhenPathIsEmpty_ThrowsArgumentException()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PatchAsync("device-1", "", JsonValue("\"off\"")));
    }

    [Fact]
    public async Task PatchAsync_WhenPathDoesNotExist_ThrowsArgumentException()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PatchAsync("device-1", "managementPoints[0].missing.value", JsonValue("\"off\"")));

        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PatchAsync_WhenValueFieldIsNotSettable_ThrowsArgumentException()
    {
        Device device = BuildDevice();
        device.ManagementPoints[0].OnOffMode!.Settable = false;
        FakeDeviceRepository repository = new(device);
        DeviceService service = new(repository);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PatchAsync("device-1", "managementPoints[0].onOffMode.value", JsonValue("\"off\"")));

        Assert.Contains("niet aanpasbaar", exception.Message);
    }

    [Fact]
    public async Task PatchAsync_WhenValueIsNotInAllowedValues_ThrowsArgumentException()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PatchAsync("device-1", "managementPoints[0].operationMode.value", JsonValue("\"dry\"")));

        Assert.Contains("Toegestane waarden", exception.Message);
    }

    [Fact]
    public async Task PatchAsync_WhenTemperatureIsAboveMaximum_ThrowsArgumentException()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PatchAsync("device-1", "managementPoints[0].targetTemperature.value", JsonValue("31")));

        Assert.Contains("<= 30", exception.Message);
    }

    [Fact]
    public async Task PatchAsync_WhenTemperatureDoesNotMatchStep_ThrowsArgumentException()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PatchAsync("device-1", "managementPoints[0].targetTemperature.value", JsonValue("22.3")));

        Assert.Contains("stappen van", exception.Message);
    }

    [Fact]
    public async Task PatchAsync_WhenOperationModeChanges_SynchronizesTargetTemperature()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        Device? updated = await service.PatchAsync(
            "device-1",
            "managementPoints[0].operationMode.value",
            JsonValue("\"cooling\""));

        Assert.NotNull(updated);
        ManagementPoint primary = updated.ManagementPoints[0];
        Assert.Equal("cooling", primary.OperationMode!.Value);
        Assert.Equal(24, primary.TargetTemperature!.Value);
        Assert.Equal(15, primary.TargetTemperature.MinValue);
        Assert.Equal(35, primary.TargetTemperature.MaxValue);
        Assert.Equal(0.5, primary.TargetTemperature.StepValue);
    }

    [Fact]
    public async Task PatchAsync_WhenActiveModeSetpointChanges_SynchronizesTargetTemperature()
    {
        FakeDeviceRepository repository = new(BuildDevice());
        DeviceService service = new(repository);

        Device? updated = await service.PatchAsync(
            "device-1",
            "managementPoints[0].temperatureControl.value.operationModes.heating.setpoints.roomTemperature.value",
            JsonValue("22.5"));

        Assert.NotNull(updated);
        ManagementPoint primary = updated.ManagementPoints[0];
        Assert.Equal("heating", primary.OperationMode!.Value);
        Assert.Equal(22.5, primary.TargetTemperature!.Value);
    }

    private static JsonElement JsonValue(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static Device BuildDevice(string id = "device-1")
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
                    ManagementPointCategory = "primary",
                    Name = new SettableStringValue
                    {
                        Settable = false,
                        Value = "Reception",
                        MaxLength = 63
                    },
                    OnOffMode = new OnOffModeField
                    {
                        Settable = true,
                        Values = new List<string> { "on", "off" },
                        Value = "on"
                    },
                    OperationMode = new OperationModeField
                    {
                        Settable = true,
                        Values = new List<string> { "heating", "cooling", "auto" },
                        Value = "heating"
                    },
                    TargetTemperature = new TargetTemperatureField
                    {
                        Settable = true,
                        Value = 21,
                        MinValue = 12,
                        MaxValue = 30,
                        StepValue = 0.5
                    },
                    TemperatureControl = new TemperatureControlField
                    {
                        Ref = "#temperatureControl",
                        Settable = false,
                        Value = new TemperatureControlValue
                        {
                            OperationModes = new Dictionary<string, OperationModeSetpoints>
                            {
                                ["heating"] = ModeSetpoints(21, 12, 30),
                                ["cooling"] = ModeSetpoints(24, 15, 35),
                                ["auto"] = ModeSetpoints(22, 12, 30)
                            }
                        }
                    }
                }
            }
        };
    }

    private static OperationModeSetpoints ModeSetpoints(double value, double min, double max)
    {
        return new OperationModeSetpoints
        {
            Setpoints = new Dictionary<string, Setpoint>
            {
                ["roomTemperature"] = new()
                {
                    Settable = true,
                    Value = value,
                    MinValue = min,
                    MaxValue = max,
                    StepValue = 0.5
                }
            }
        };
    }
}
