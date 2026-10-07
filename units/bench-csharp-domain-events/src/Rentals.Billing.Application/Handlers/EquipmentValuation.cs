namespace Rentals.Billing.Application.Handlers;

/// <summary>The catalogue service's answer to "what does it cost to replace this item".</summary>
public sealed record EquipmentValuation(Guid EquipmentId, decimal ReplacementValue, string Currency);
