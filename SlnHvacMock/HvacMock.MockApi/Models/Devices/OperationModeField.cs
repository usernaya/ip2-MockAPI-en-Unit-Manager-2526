namespace HvacMock.MockApi.Models.Devices
{
    public class OperationModeField
    {
        public bool Settable { get; set; }
        public List<string> Values { get; set; } = new();
        public string Value { get; set; } = string.Empty;
    }
}
