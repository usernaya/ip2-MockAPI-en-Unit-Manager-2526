namespace HvacMockApi.Models
{
    public class Device
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string DeviceModel { get; set; }
        public CloudConnection IsCloudConnectionUp { get; set; }
        public List<ManagementPoint> ManagementPoints { get; set; }
    }

    public class CloudConnection
    {
        public bool Value { get; set; }
    }

    public class ManagementPoint
    {
        public string EmbeddedId { get; set; }
        public string ManagementPointType { get; set; }
        public Name Name { get; set; }
        public Mode OnOffMode { get; set; }
        public Mode OperationMode { get; set; }
        public Temperature TargetTemperature { get; set; }
        public SensoryData SensoryData { get; set; }
    }

    public class Name
    {
        public string Value { get; set; }
    }

    public class Mode
    {
        public string Value { get; set; }
    }

    public class Temperature
    {
        public int Value { get; set; }
    }

    public class SensoryData
    {
        public Temperature RoomTemperature { get; set; }
    }
}