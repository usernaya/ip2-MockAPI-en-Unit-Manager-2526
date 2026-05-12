namespace HvacMock.UI.Models
{
    public class DashboardViewModel
    {
        public int TotalDevices { get; set; }
        public int ActiveDevices { get; set; }
        public int InactiveDevices { get; set; }
        public double AverageTemperature { get; set; }

        // Aantal devices per operationMode (heating / cooling / auto / -)
        public Dictionary<string, int> ModeDistribution { get; set; } = new();

        // Aantal devices per type (heating / airConditioner / cooling)
        public Dictionary<string, int> TypeDistribution { get; set; } = new();

        // Volledige lijst voor de "recent" tabel onderaan
        public List<Device> Devices { get; set; } = new();
    }
}
