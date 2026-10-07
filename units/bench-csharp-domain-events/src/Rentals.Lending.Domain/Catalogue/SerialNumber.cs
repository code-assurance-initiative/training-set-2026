namespace Rentals.Lending.Domain.Catalogue;

/// <summary>The manufacturer's serial number of one physical unit, normalised to upper case.</summary>
public sealed record SerialNumber
{
    public SerialNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalised = value.Trim().ToUpperInvariant();
        if (normalised.Length > 40 || !normalised.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
        {
            throw new ArgumentException("A serial number is up to 40 letters, digits or dashes.", nameof(value));
        }

        Value = normalised;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
