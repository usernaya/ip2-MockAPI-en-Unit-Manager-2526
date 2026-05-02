using HvacMock.MockApi.Models;
using HvacMock.MockApi.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HvacMock.MockApi.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _repo;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public DeviceService(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Device?> GetByIdAsync(string deviceId)
        {
            return await _repo.GetByIdAsync(deviceId);
        }

        public async Task<Device?> PatchAsync(string deviceId, string path, JsonElement newValue)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Het pad mag niet leeg zijn. Voorbeeld: managementPoints[1].targetTemperature.value");
            }

            Device? device = await _repo.GetByIdAsync(deviceId);

            if (device == null)
            {
                return null;
            }

            // Serialiseer het device naar JSON zodat we er met JsonNode op kunnen navigeren
            string json = JsonSerializer.Serialize(device, JsonOptions);
            JsonNode root = JsonNode.Parse(json)!;

            // Navigeer naar het parent-node en zet de nieuwe waarde
            (JsonNode parentNode, string lastSegment) = NavigateToParent(root, path);

            ValidateAgainstConstraints(parentNode, lastSegment, newValue);

            parentNode[lastSegment] = JsonNode.Parse(newValue.GetRawText());

            // Deserialize terug naar Device en sla op in DynamoDB
            string updatedJson = root.ToJsonString();
            Device updatedDevice = JsonSerializer.Deserialize<Device>(updatedJson, JsonOptions)!;

            ApplyOperationModeDefaultTemperature(updatedDevice, path, newValue);

            await _repo.SaveAsync(updatedDevice);

            return updatedDevice;
        }

        private static void ApplyOperationModeDefaultTemperature(Device device, string path, JsonElement newValue)
        {
            Match match = Regex.Match(path, @"^managementPoints\[(\d+)\]\.operationMode\.value$");
            if (!match.Success)
            {
                return;
            }

            string? mode = newValue.ValueKind == JsonValueKind.String ? newValue.GetString() : null;
            if (string.IsNullOrWhiteSpace(mode))
            {
                throw new ArgumentException("Operation mode moet een tekstwaarde zijn.");
            }

            int managementPointIndex = int.Parse(match.Groups[1].Value);

            if (managementPointIndex < 0 || managementPointIndex >= device.ManagementPoints.Count)
            {
                throw new ArgumentException($"Management point index {managementPointIndex} bestaat niet.");
            }

            ManagementPoint managementPoint = device.ManagementPoints[managementPointIndex];
            TargetTemperatureField targetTemperature = managementPoint.TargetTemperature
                ?? throw new ArgumentException("targetTemperature ontbreekt op dit management point.");

            if (managementPoint.TemperatureControl?.Value.OperationModes.TryGetValue(mode, out OperationModeSetpoints? modeSetpoints) != true
                || modeSetpoints == null
                || !modeSetpoints.Setpoints.TryGetValue("roomTemperature", out Setpoint? roomTemperature))
            {
                throw new ArgumentException($"Geen standaardtemperatuur gevonden voor mode '{mode}'.");
            }

            ValidateTemperatureValue(roomTemperature.MinValue, roomTemperature.MaxValue, roomTemperature.StepValue, roomTemperature.Value);

            targetTemperature.MinValue = roomTemperature.MinValue;
            targetTemperature.MaxValue = roomTemperature.MaxValue;
            targetTemperature.StepValue = roomTemperature.StepValue;
            targetTemperature.Value = roomTemperature.Value;
        }

        private static void ValidateTemperatureValue(double minValue, double maxValue, double stepValue, double value)
        {
            if (value < minValue)
                throw new ArgumentException($"Waarde moet >= {minValue} zijn.");

            if (value > maxValue)
                throw new ArgumentException($"Waarde moet <= {maxValue} zijn.");

            if (stepValue > 0)
            {
                double steps = (value - minValue) / stepValue;

                if (Math.Abs(steps - Math.Round(steps)) > 0.000001)
                    throw new ArgumentException($"Waarde moet in stappen van {stepValue} zijn.");
            }
        }

        // Leest settable / values / minValue / maxValue uit de parent-node en gooit ArgumentException als de waarde ongeldig is.
        // Werkt alleen als het laatste segment "value" is (bv. onOffMode.value, targetTemperature.value).
        private static void ValidateAgainstConstraints(JsonNode parentNode, string lastSegment, JsonElement newValue)
        {
            if (lastSegment != "value") return;
            if (parentNode is not JsonObject fieldObj) return;

            // settable check
            if (fieldObj["settable"] is JsonValue settableNode && !settableNode.GetValue<bool>())
                throw new ArgumentException("Dit veld is niet aanpasbaar (settable: false).");

            // allowed values check
            if (fieldObj["values"] is JsonArray allowedValues && allowedValues.Count > 0)
            {
                string? incoming = newValue.ValueKind == JsonValueKind.String ? newValue.GetString() : null;
                if (!allowedValues.Any(v => v?.GetValue<string>() == incoming))
                {
                    string joined = string.Join(", ", allowedValues.Select(v => v?.GetValue<string>() ?? ""));
                    throw new ArgumentException($"Ongeldige waarde. Toegestane waarden: {joined}.");
                }
            }

            // min/max check
            JsonNode? minNode = fieldObj["minValue"];
            JsonNode? maxNode = fieldObj["maxValue"];
            JsonNode? stepNode = fieldObj["stepValue"];
            if (minNode != null || maxNode != null || stepNode != null)
            {
                if (newValue.ValueKind != JsonValueKind.Number || !newValue.TryGetDouble(out double numVal))
                    throw new ArgumentException("Waarde moet een getal zijn.");

                if (minNode != null && numVal < minNode.GetValue<double>())
                    throw new ArgumentException($"Waarde moet >= {minNode.GetValue<double>()} zijn.");

                if (maxNode != null && numVal > maxNode.GetValue<double>())
                    throw new ArgumentException($"Waarde moet <= {maxNode.GetValue<double>()} zijn.");

                if (stepNode != null && stepNode.GetValue<double>() > 0)
                {
                    double minVal = minNode?.GetValue<double>() ?? 0;
                    double stepVal = stepNode.GetValue<double>();
                    double steps = (numVal - minVal) / stepVal;

                    if (Math.Abs(steps - Math.Round(steps)) > 0.000001)
                        throw new ArgumentException($"Waarde moet in stappen van {stepVal} zijn.");
                }
            }
        }

        // Navigeert naar het parent-node van het doelveld.
        // Gooit ArgumentException als een segment niet bestaat of een index buiten bereik valt.
        // Voorbeeld pad: "managementPoints[0].targetTemperature.value"
        //   → segments: ["managementPoints[0]", "targetTemperature", "value"]
        //   → return: (node van "targetTemperature", "value")
        private static (JsonNode parent, string lastSegment) NavigateToParent(JsonNode root, string path)
        {
            string[] segments = path.Split('.');

            JsonNode current = root;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                current = ResolveSegment(current, segments[i]);
            }

            // Controleer of het laatste segment bestaat
            string last = segments[^1];
            string lastKey = ExtractPropertyName(last);

            if (current is JsonObject obj && !obj.ContainsKey(lastKey))
            {
                throw new ArgumentException($"Field '{lastKey}' not found");
            }

            return (current, lastKey);
        }

        // Verwerkt één pad-segment: property-naam of array-index (bv. "managementPoints[0]")
        private static JsonNode ResolveSegment(JsonNode current, string segment)
        {
            // Controleer of dit segment een array-index bevat: bv. "managementPoints[0]"
            Match arrayMatch = Regex.Match(segment, @"^(.+)\[(\d+)\]$");

            if (arrayMatch.Success)
            {
                string propertyName = arrayMatch.Groups[1].Value;
                int index = int.Parse(arrayMatch.Groups[2].Value);

                JsonNode? arrayNode = current[propertyName];
                if (arrayNode == null)
                {
                    throw new ArgumentException($"Property '{propertyName}' not found");
                }

                if (arrayNode is not JsonArray jsonArray)
                {
                    throw new ArgumentException($"Property '{propertyName}' is not an array");
                }

                if (index < 0 || index >= jsonArray.Count)
                {
                    throw new ArgumentException($"Index {index} out of range for '{propertyName}' (length: {jsonArray.Count})");
                }

                return jsonArray[index]!;
            }

            // Gewone property-naam
            JsonNode? next = current[segment];
            if (next == null)
            {
                throw new ArgumentException($"Property '{segment}' not found");
            }

            return next;
        }

        // Haalt de property-naam op uit een segment dat eventueel een array-index bevat
        private static string ExtractPropertyName(string segment)
        {
            Match arrayMatch = Regex.Match(segment, @"^(.+)\[(\d+)\]$");
            return arrayMatch.Success ? arrayMatch.Groups[1].Value : segment;
        }
    }
}
