using HvacMock.MockApi.Models;
using HvacMock.MockApi.Repositories;

namespace HvacMock.MockApi.Infrastructure
{
    public class DataSeeder
    {
        private readonly IDeviceRepository _repo;

        public DataSeeder(IDeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task SeedAsync()
        {
            // Maak tabel aan als die nog niet bestaat
            await _repo.CreateTableIfNotExistsAsync();

            // Sla seeding over als er al data aanwezig is (bestaande state niet overschrijven)
            List<Device> existing = await _repo.GetAllAsync();
            if (existing.Count > 0)
            {
                return;
            }

            List<Device> seedDevices = BuildSeedDevices();

            foreach (Device device in seedDevices)
            {
                await _repo.SaveAsync(device);
            }
        }

        private static List<Device> BuildSeedDevices()
        {
            return new List<Device>
            {
                new Device
                {
                    Id = "device-1",
                    Type = "heating",
                    DeviceModel = "Altherma",
                    ManagementPoints = new List<ManagementPoint>
                    {
                        // Gateway (secundair, geen settable velden voor ons)
                        new ManagementPoint
                        {
                            EmbeddedId = "0",
                            ManagementPointType = "gateway",
                            ManagementPointCategory = "secondary",
                            Name = new SettableStringValue
                            {
                                Settable = false,
                                Value = "Gateway",
                                MaxLength = 63
                            }
                        },
                        // ClimateControl (primair, de management point die PATCH ondersteunt)
                        new ManagementPoint
                        {
                            EmbeddedId = "1",
                            ManagementPointType = "climateControl",
                            ManagementPointCategory = "primary",
                            Name = new SettableStringValue
                            {
                                Settable = false,
                                Value = "Woonkamer",
                                MaxLength = 63
                            },
                            OnOffMode = new OnOffModeField
                            {
                                Settable = true,
                                Values = new List<string> { "on", "off" },
                                Value = "on"
                            },
                            OperationMode = new OperationModeField
                            {
                                Settable = true,
                                Values = new List<string> { "heating", "cooling", "auto" },
                                Value = "heating"
                            },
                            TargetTemperature = new TargetTemperatureField
                            {
                                Settable = true,
                                Value = 21,
                                MinValue = 12,
                                MaxValue = 30,
                                StepValue = 0.5
                            },
                            TemperatureControl = new TemperatureControlField
                            {
                                Ref = "#temperatureControl",
                                Settable = false,
                                Value = new TemperatureControlValue
                                {
                                    OperationModes = new Dictionary<string, OperationModeSetpoints>
                                    {
                                        ["heating"] = new OperationModeSetpoints
                                        {
                                            Setpoints = new Dictionary<string, Setpoint>
                                            {
                                                ["roomTemperature"] = new Setpoint
                                                {
                                                    Settable = true,
                                                    Value = 21,
                                                    MinValue = 12,
                                                    MaxValue = 30,
                                                    StepValue = 0.5
                                                }
                                            }
                                        },
                                        ["cooling"] = new OperationModeSetpoints
                                        {
                                            Setpoints = new Dictionary<string, Setpoint>
                                            {
                                                ["roomTemperature"] = new Setpoint
                                                {
                                                    Settable = true,
                                                    Value = 24,
                                                    MinValue = 15,
                                                    MaxValue = 35,
                                                    StepValue = 0.5
                                                }
                                            }
                                        },
                                        ["auto"] = new OperationModeSetpoints
                                        {
                                            Setpoints = new Dictionary<string, Setpoint>
                                            {
                                                ["roomTemperature"] = new Setpoint
                                                {
                                                    Settable = true,
                                                    Value = 21,
                                                    MinValue = 12,
                                                    MaxValue = 30,
                                                    StepValue = 0.5
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new Device
                {
                    Id = "device-2",
                    Type = "airConditioner",
                    DeviceModel = "FTXM35R",
                    ManagementPoints = new List<ManagementPoint>
                    {
                        new ManagementPoint
                        {
                            EmbeddedId = "0",
                            ManagementPointType = "gateway",
                            ManagementPointCategory = "secondary",
                            Name = new SettableStringValue
                            {
                                Settable = false,
                                Value = "Gateway",
                                MaxLength = 63
                            }
                        },
                        new ManagementPoint
                        {
                            EmbeddedId = "1",
                            ManagementPointType = "climateControl",
                            ManagementPointCategory = "primary",
                            Name = new SettableStringValue
                            {
                                Settable = false,
                                Value = "Slaapkamer",
                                MaxLength = 63
                            },
                            OnOffMode = new OnOffModeField
                            {
                                Settable = true,
                                Values = new List<string> { "on", "off" },
                                Value = "off"
                            },
                            OperationMode = new OperationModeField
                            {
                                Settable = true,
                                Values = new List<string> { "heating", "cooling", "auto" },
                                Value = "cooling"
                            },
                            TargetTemperature = new TargetTemperatureField
                            {
                                Settable = true,
                                Value = 22,
                                MinValue = 12,
                                MaxValue = 30,
                                StepValue = 0.5
                            },
                            TemperatureControl = new TemperatureControlField
                            {
                                Ref = "#temperatureControl",
                                Settable = false,
                                Value = new TemperatureControlValue
                                {
                                    OperationModes = new Dictionary<string, OperationModeSetpoints>
                                    {
                                        ["heating"] = new OperationModeSetpoints
                                        {
                                            Setpoints = new Dictionary<string, Setpoint>
                                            {
                                                ["roomTemperature"] = new Setpoint
                                                {
                                                    Settable = true,
                                                    Value = 20,
                                                    MinValue = 12,
                                                    MaxValue = 30,
                                                    StepValue = 0.5
                                                }
                                            }
                                        },
                                        ["cooling"] = new OperationModeSetpoints
                                        {
                                            Setpoints = new Dictionary<string, Setpoint>
                                            {
                                                ["roomTemperature"] = new Setpoint
                                                {
                                                    Settable = true,
                                                    Value = 22,
                                                    MinValue = 15,
                                                    MaxValue = 35,
                                                    StepValue = 0.5
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
