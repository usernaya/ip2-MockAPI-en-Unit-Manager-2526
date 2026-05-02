namespace HvacMock.MockApi.Models
{
    public static class DeviceConstraints
    {
        public static readonly Dictionary<string, FieldConstraint> Fields = new()
        {
            ["mode"]        = new(Settable: true,  AllowedValues: ["heating", "cooling", "auto"]),
            ["status"]      = new(Settable: true,  AllowedValues: ["on", "off", "standby"]),
            ["temperature"] = new(Settable: true,  Min: 16, Max: 30),
            ["deviceModel"] = new(Settable: false),
            ["type"]        = new(Settable: false),
        };
    }
}
