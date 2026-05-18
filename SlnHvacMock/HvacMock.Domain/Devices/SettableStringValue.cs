namespace HvacMock.Domain.Devices
{
    public class SettableStringValue
    {
        public bool Settable { get; set; }
        public string Value { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
    }
}
