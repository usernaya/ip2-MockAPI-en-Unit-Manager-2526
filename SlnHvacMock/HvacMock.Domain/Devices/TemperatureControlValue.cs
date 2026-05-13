namespace HvacMock.Domain.Devices
{
    public class TemperatureControlValue
    {
        public Dictionary<string, OperationModeSetpoints> OperationModes { get; set; } = new();
    }
}
