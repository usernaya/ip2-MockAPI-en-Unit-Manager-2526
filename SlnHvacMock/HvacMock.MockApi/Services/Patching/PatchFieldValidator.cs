using System.Text.Json;
using System.Text.Json.Nodes;

namespace HvacMock.MockApi.Services.Patching
{
    internal static class PatchFieldValidator
    {
        public static void ValidateAgainstConstraints(JsonNode parentNode, string lastSegment, JsonElement newValue)
        {
            if (lastSegment != "value") return;
            if (parentNode is not JsonObject fieldObj) return;

            if (fieldObj["settable"] is JsonValue settableNode && !settableNode.GetValue<bool>())
                throw new ArgumentException("Dit veld is niet aanpasbaar (settable: false).");

            if (fieldObj["values"] is JsonArray allowedValues && allowedValues.Count > 0)
            {
                string? incoming = newValue.ValueKind == JsonValueKind.String ? newValue.GetString() : null;
                if (!allowedValues.Any(v => v?.GetValue<string>() == incoming))
                {
                    string joined = string.Join(", ", allowedValues.Select(v => v?.GetValue<string>() ?? ""));
                    throw new ArgumentException($"Ongeldige waarde. Toegestane waarden: {joined}.");
                }
            }

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

        public static void ValidateTemperatureValue(double minValue, double maxValue, double stepValue, double value)
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
    }
}
