namespace HvacMock.Domain.Devices
{
    public class Device
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public List<ManagementPoint> ManagementPoints { get; set; } = new();
    }
}
