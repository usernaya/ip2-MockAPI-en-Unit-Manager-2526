namespace HvacMock.Domain.Devices
{
    public class TemperatureControlField
    {
        public string? Ref { get; set; }
        public bool Settable { get; set; }
        public TemperatureControlValue Value { get; set; } = new();
    }
}
