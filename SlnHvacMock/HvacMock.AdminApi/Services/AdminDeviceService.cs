using HvacMock.AdminApi.DTOs;
using HvacMock.AdminApi.Models;
using HvacMock.AdminApi.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HvacMock.AdminApi.Services
{
    public class AdminDeviceService : IAdminDeviceService
    {
        private readonly IDeviceRepository _repo;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public AdminDeviceService(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Device?> GetByIdAsync(string id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<Device> CreateAsync(CreateDeviceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                throw new ArgumentException("Een device moet een Id hebben.");
            }

            Device device = new Device
            {
                Id = request.Id,
                Type = request.Type,
                DeviceModel = request.DeviceModel,
                ManagementPoints = request.ManagementPoints
            };

            await _repo.SaveAsync(device);
            return device;
        }

        public async Task<Device?> PatchAsync(string id, PatchRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Path))
            {
                throw new ArgumentException("Het pad mag niet leeg zijn. Voorbeeld: managementPoints[0].targetTemperature.value");
            }

            Device? device = await _repo.GetByIdAsync(id);
            if (device == null)
            {
                return null;
            }

            // Serialiseer naar JSON zodat we met JsonNode kunnen navigeren
            string json = JsonSerializer.Serialize(device, JsonOptions);
            JsonNode root = JsonNode.Parse(json)!;

            // Navigeer naar het parent-node en zet de nieuwe waarde
            (JsonNode parentNode, string lastSegment) = NavigateToParent(root, req.Path);
            parentNode[lastSegment] = JsonNode.Parse(req.Value.GetRawText());

            // Deserialize terug naar Device en sla op
            string updatedJson = root.ToJsonString();
            Device updatedDevice = JsonSerializer.Deserialize<Device>(updatedJson, JsonOptions)!;

            await _repo.SaveAsync(updatedDevice);
            return updatedDevice;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            Device? device = await _repo.GetByIdAsync(id);
            if (device == null)
            {
                return false;
            }

            await _repo.DeleteAsync(id);
            return true;
        }

        // Navigeert naar het parent-node van het doelveld.
        // Voorbeeld pad: "managementPoints[0].targetTemperature.value"
        //   → return: (node van "targetTemperature", "value")
        private static (JsonNode parent, string lastSegment) NavigateToParent(JsonNode root, string path)
        {
            string[] segments = path.Split('.');
            JsonNode current = root;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                current = ResolveSegment(current, segments[i]);
            }

            string last = segments[^1];
            string lastKey = ExtractPropertyName(last);

            if (current is JsonObject obj && !obj.ContainsKey(lastKey))
            {
                throw new ArgumentException($"Veld '{lastKey}' bestaat niet in dit device.");
            }

            return (current, lastKey);
        }

        // Verwerkt één pad-segment: property-naam of array-index (bv. "managementPoints[0]")
        private static JsonNode ResolveSegment(JsonNode current, string segment)
        {
            Match arrayMatch = Regex.Match(segment, @"^(.+)\[(\d+)\]$");

            if (arrayMatch.Success)
            {
                string propertyName = arrayMatch.Groups[1].Value;
                int index = int.Parse(arrayMatch.Groups[2].Value);

                JsonNode? arrayNode = current[propertyName];
                if (arrayNode == null)
                {
                    throw new ArgumentException($"Property '{propertyName}' bestaat niet.");
                }

                if (arrayNode is not JsonArray jsonArray)
                {
                    throw new ArgumentException($"Property '{propertyName}' is geen array.");
                }

                if (index < 0 || index >= jsonArray.Count)
                {
                    throw new ArgumentException($"Index {index} valt buiten bereik van '{propertyName}' (lengte: {jsonArray.Count}).");
                }

                return jsonArray[index]!;
            }

            JsonNode? next = current[segment];
            if (next == null)
            {
                throw new ArgumentException($"Property '{segment}' bestaat niet.");
            }

            return next;
        }

        private static string ExtractPropertyName(string segment)
        {
            Match arrayMatch = Regex.Match(segment, @"^(.+)\[(\d+)\]$");
            return arrayMatch.Success ? arrayMatch.Groups[1].Value : segment;
        }
    }
}
