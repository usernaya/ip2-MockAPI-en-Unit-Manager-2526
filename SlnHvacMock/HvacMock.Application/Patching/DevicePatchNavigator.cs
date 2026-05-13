using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HvacMock.Application.Patching
{
    internal static class DevicePatchNavigator
    {
        public static (JsonNode parent, string lastSegment) NavigateToParent(JsonNode root, string path)
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
                throw new ArgumentException($"Field '{lastKey}' not found");
            }

            return (current, lastKey);
        }

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

            JsonNode? next = current[segment];
            if (next == null)
            {
                throw new ArgumentException($"Property '{segment}' not found");
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
