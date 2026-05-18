using HvacMock.Domain.Devices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HvacMock.Application.Patching
{
    internal static class OperationModeTemperatureUpdater
    {
        public static void Apply(Device device, string path, JsonElement newValue)
        {
            Match operationModeMatch = Regex.Match(path, @"^managementPoints\[(\d+)\]\.operationMode\.value$");
            if (operationModeMatch.Success)
            {
                ApplyOperationModeChange(device, operationModeMatch, newValue);
                return;
            }

            Match setpointMatch = Regex.Match(path, @"^managementPoints\[(\d+)\]\.temperatureControl\.value\.operationModes\.([^.]+)\.setpoints\.roomTemperature\.(value|minValue|maxValue|stepValue)$");
            if (setpointMatch.Success)
            {
                ApplyActiveSetpointChange(device, setpointMatch);
            }
        }

        private static void ApplyOperationModeChange(Device device, Match match, JsonElement newValue)
        {
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

        private static void ApplyActiveSetpointChange(Device device, Match match)
        {
            int managementPointIndex = int.Parse(match.Groups[1].Value);
            string mode = match.Groups[2].Value;
            string changedField = match.Groups[3].Value;

            if (managementPointIndex < 0 || managementPointIndex >= device.ManagementPoints.Count)
            {
                throw new ArgumentException($"Management point index {managementPointIndex} bestaat niet.");
            }

            ManagementPoint managementPoint = device.ManagementPoints[managementPointIndex];

            if (managementPoint.OperationMode?.Value != mode)
            {
                return;
            }

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

            if (changedField == "value")
            {
                targetTemperature.Value = roomTemperature.Value;
            }
        }
    }
}
