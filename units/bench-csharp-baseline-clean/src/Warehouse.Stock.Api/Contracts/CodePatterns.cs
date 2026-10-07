namespace Warehouse.Stock.Api.Contracts;

/// <summary>The accepted shapes of identifiers. Anchored and free of nested quantifiers, so matching is linear.</summary>
public static class CodePatterns
{
    /// <summary>Upper-case letters, digits and dashes, 3–32 characters, e.g. <c>BOLT-M8-40</c>.</summary>
    public const string Sku = "^[A-Z0-9][A-Z0-9-]{2,31}$";

    /// <summary>Aisle letter, then two-digit aisle, rack and shelf numbers, e.g. <c>B04-12-03</c>.</summary>
    public const string Bin = "^[A-Z][0-9]{2}-[0-9]{2}-[0-9]{2}$";

    /// <summary>One to four upper-case letters, e.g. <c>COLD</c>.</summary>
    public const string Zone = "^[A-Z]{1,4}$";

    /// <summary>One to eight upper-case letters, e.g. <c>EA</c> or <c>BOX</c>.</summary>
    public const string UnitOfMeasure = "^[A-Z]{1,8}$";

    public const int MaximumQuantity = 1_000_000;
}
