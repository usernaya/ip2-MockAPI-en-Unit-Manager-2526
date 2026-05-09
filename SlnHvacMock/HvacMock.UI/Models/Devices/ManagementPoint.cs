namespace HvacMock.UI.Models.Devices
{
    public class ManagementPoint
    {
        public string EmbeddedId { get; set; } = string.Empty;
        public string ManagementPointType { get; set; } = string.Empty;
        public string ManagementPointCategory { get; set; } = string.Empty;
        public SettableStringValue? Name { get; set; }
        public OnOffModeField? OnOffMode { get; set; }
        public OperationModeField? OperationMode { get; set; }
        public TargetTemperatureField? TargetTemperature { get; set; }
        public TemperatureControlField? TemperatureControl { get; set; }
    }
}
