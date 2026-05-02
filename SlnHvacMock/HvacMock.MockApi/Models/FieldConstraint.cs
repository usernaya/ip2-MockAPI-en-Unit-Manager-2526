namespace HvacMock.MockApi.Models
{
    public record FieldConstraint(
        bool Settable,
        string[]? AllowedValues = null,
        double? Min = null,
        double? Max = null
    );
}
