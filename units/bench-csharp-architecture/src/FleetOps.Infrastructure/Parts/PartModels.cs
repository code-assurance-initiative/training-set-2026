namespace FleetOps.Infrastructure.Parts;

public enum PartAvailability
{
    InStock,
    BackOrdered,
    Discontinued,
}

public enum PartCategory
{
    Filters,
    Brakes,
    Electrical,
    Body,
    Engine,
}

public sealed record Part(PartNumber Number, string Description, PartCategory Category);

public sealed record PartQuote(PartNumber Number, decimal UnitPrice, PartAvailability Availability, int LeadTimeDays);

public sealed record PartOrderLine(PartNumber Number, int Quantity);

public sealed record PartOrder(string OrderNumber, IReadOnlyList<PartOrderLine> Lines);
