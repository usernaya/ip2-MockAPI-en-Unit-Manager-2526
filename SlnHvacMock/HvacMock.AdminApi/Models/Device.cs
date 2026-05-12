namespace HvacMock.AdminApi.Models
{
    public class Device
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public List<ManagementPoint> ManagementPoints { get; set; } = new();
    }

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

    // { "settable": false, "value": "Gateway", "maxLength": 63 }
    public class SettableStringValue
    {
        public bool Settable { get; set; }
        public string Value { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
    }

    // { "settable": true, "values": ["on","off"], "value": "on" }
    public class OnOffModeField
    {
        public bool Settable { get; set; }
        public List<string> Values { get; set; } = new();
        public string Value { get; set; } = string.Empty;
    }

    // { "settable": true, "values": ["heating","cooling","auto"], "value": "heating" }
    public class OperationModeField
    {
        public bool Settable { get; set; }
        public List<string> Values { get; set; } = new();
        public string Value { get; set; } = string.Empty;
    }

    // { "settable": true, "value": 21, "minValue": 12, "maxValue": 30, "stepValue": 0.5 }
    public class TargetTemperatureField
    {
        public bool Settable { get; set; }
        public double Value { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public double StepValue { get; set; }
    }

    // { "ref": "#temperatureControl", "settable": false, "value": { "operationModes": { ... } } }
    public class TemperatureControlField
    {
        public string? Ref { get; set; }
        public bool Settable { get; set; }
        public TemperatureControlValue Value { get; set; } = new();
    }

    public class TemperatureControlValue
    {
        public Dictionary<string, OperationModeSetpoints> OperationModes { get; set; } = new();
    }

    // { "setpoints": { "roomTemperature": { ... } } }
    public class OperationModeSetpoints
    {
        public Dictionary<string, Setpoint> Setpoints { get; set; } = new();
    }

    // { "settable": true, "value": 21, "minValue": 12, "maxValue": 30, "stepValue": 0.5 }
    public class Setpoint
    {
        public bool Settable { get; set; }
        public double Value { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public double StepValue { get; set; }
    }
}
