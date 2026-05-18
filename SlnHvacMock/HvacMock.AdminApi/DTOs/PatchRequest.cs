using System.Text.Json;

namespace HvacMock.AdminApi.DTOs
{
    // Voorbeeld: { "path": "managementPoints[0].targetTemperature.value", "value": 22 }
    public class PatchRequest
    {
        public string Path { get; set; } = string.Empty;
        public JsonElement Value { get; set; }
    }
}
