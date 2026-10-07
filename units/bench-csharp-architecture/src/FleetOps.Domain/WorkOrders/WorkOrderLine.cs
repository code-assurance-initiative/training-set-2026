namespace FleetOps.Domain.WorkOrders;

/// <summary>A labour line (quantity in hours) or a part line (quantity in units).</summary>
public sealed record WorkOrderLine(LineKind Kind, string Description, decimal Quantity, decimal UnitPrice);
