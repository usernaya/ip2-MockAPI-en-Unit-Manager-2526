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
            parentNode[lastSegment] = JsonNode.Parse(newValue.GetRawText());

            // Deserialize terug naar Device en sla op in DynamoDB
            string updatedJson = root.ToJsonString();
            Device updatedDevice = JsonSerializer.Deserialize<Device>(updatedJson, JsonOptions)!;

            await _repo.SaveAsync(updatedDevice);

            return updatedDevice;
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
