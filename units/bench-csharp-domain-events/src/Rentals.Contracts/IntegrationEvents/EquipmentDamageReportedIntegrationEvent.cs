using Rentals.Lending.Domain.Catalogue;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Contracts.IntegrationEvents;

/// <summary>A unit came back damaged or was reported lost. Billing charges the member; Lending withdraws the unit.</summary>
public sealed record EquipmentDamageReportedIntegrationEvent(
    Guid LoanId,
    Guid MemberId,
    Guid EquipmentId,
    Guid UnitId,
    UnitCondition Condition,
    Money ReplacementValue,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
