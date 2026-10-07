using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;
using Rentals.Lending.Domain.Members;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Infrastructure.Messaging;

/// <summary>Translates the domain events other contexts need into integration events. Others stay inside Lending.</summary>
internal static class IntegrationEventMapper
{
    public static IEnumerable<IIntegrationEvent> Map(IAggregateRoot source, IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case MemberRegistered registered when source is Member member:
                yield return new MemberRegisteredIntegrationEvent(
                    registered.MemberId.Value, member.FullName, member.Email, registered.OccurredAt);
                break;
            case LoanOpened opened:
                yield return new LoanOpenedIntegrationEvent(
                    opened.LoanId.Value, opened.MemberId.Value, opened.EquipmentId.Value, opened.DueAt, opened.OccurredAt);
                break;
            case LoanReturned returned:
                yield return new LoanReturnedIntegrationEvent(
                    returned.LoanId.Value, returned.MemberId.Value, returned.DueAt, returned.OccurredAt, returned.DailyRate,
                    returned.OccurredAt);
                if (returned.ReturnCondition is UnitCondition.Damaged or UnitCondition.Lost)
                {
                    yield return new EquipmentDamageReportedIntegrationEvent(
                        returned.LoanId.Value, returned.MemberId.Value, returned.EquipmentId.Value,
                        returned.EquipmentUnitId.Value, returned.ReturnCondition, returned.ReplacementValue,
                        returned.OccurredAt);
                }

                break;
        }
    }
}
