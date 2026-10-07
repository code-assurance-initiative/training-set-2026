namespace FleetOps.Contracts.WorkOrders;

/// <summary>One line of a work order. <see cref="Kind"/> is <c>Labour</c> (quantity in hours) or <c>Part</c>.</summary>
public sealed record WorkOrderLineSummary(string Kind, string Description, decimal Quantity, decimal UnitPrice);
