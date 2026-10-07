namespace FleetOps.Infrastructure.Parts;

/// <summary>A supplier part number: letters, digits and dashes, compared without case.</summary>
public sealed record PartNumber
{
    public PartNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
        {
            throw new ArgumentException($"'{value}' is not a part number.", nameof(value));
        }

        Value = value.ToUpperInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
