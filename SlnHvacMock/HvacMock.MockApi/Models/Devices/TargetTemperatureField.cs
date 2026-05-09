namespace HvacMock.MockApi.Models.Devices
{
    public class TargetTemperatureField
    {
        public bool Settable { get; set; }
        public double Value { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public double StepValue { get; set; }
    }
}
