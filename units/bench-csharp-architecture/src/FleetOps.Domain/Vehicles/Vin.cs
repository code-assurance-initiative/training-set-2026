using FleetOps.Domain.Common;

namespace FleetOps.Domain.Vehicles;

/// <summary>A vehicle identification number (ISO 3779): 17 characters, digits and capital letters except I, O and Q.</summary>
public sealed record Vin
{
    private const string Alphabet = "0123456789ABCDEFGHJKLMNPRSTUVWXYZ";

    public Vin(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalised = value.Trim().ToUpperInvariant();
        if (normalised.Length != 17 || normalised.Any(c => !Alphabet.Contains(c, StringComparison.Ordinal)))
        {
            throw new DomainException($"'{value}' is not a valid vehicle identification number.");
        }

        Value = normalised;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
