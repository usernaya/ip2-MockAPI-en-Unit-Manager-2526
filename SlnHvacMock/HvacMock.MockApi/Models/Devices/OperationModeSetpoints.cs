namespace HvacMock.MockApi.Models.Devices
{
    public class OperationModeSetpoints
    {
        public Dictionary<string, Setpoint> Setpoints { get; set; } = new();
    }
}
