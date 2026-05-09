namespace HvacMock.MockApi.Models.Devices.Constraints
{
    public record FieldConstraint(
        bool Settable,
        string[]? AllowedValues = null,
        double? Min = null,
        double? Max = null
    );
}
