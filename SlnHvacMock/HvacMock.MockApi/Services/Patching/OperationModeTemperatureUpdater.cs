using HvacMock.MockApi.Models.Devices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HvacMock.MockApi.Services.Patching
{
    internal static class OperationModeTemperatureUpdater
    {
        public static void Apply(Device device, string path, JsonElement newValue)
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

            PatchFieldValidator.ValidateTemperatureValue(
                roomTemperature.MinValue,
                roomTemperature.MaxValue,
                roomTemperature.StepValue,
                roomTemperature.Value);

            targetTemperature.MinValue = roomTemperature.MinValue;
            targetTemperature.MaxValue = roomTemperature.MaxValue;
            targetTemperature.StepValue = roomTemperature.StepValue;
            targetTemperature.Value = roomTemperature.Value;
        }
    }
}
