using System.Collections.Frozen;
using System.Text.Json;
using Rentals.Contracts.IntegrationEvents;
using Rentals.Messaging;

namespace Rentals.Lending.Infrastructure.Messaging;

/// <summary>Serialises the integration events Lending publishes, by a stable type name.</summary>
internal static class IntegrationEventSerializer
{
    private static readonly FrozenDictionary<string, Type> Types = new[]
    {
        typeof(MemberRegisteredIntegrationEvent),
        typeof(LoanOpenedIntegrationEvent),
        typeof(LoanReturnedIntegrationEvent),
        typeof(EquipmentDamageReportedIntegrationEvent),
    }.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static OutboxMessage ToOutbox(IIntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType();
        if (!Types.ContainsKey(type.Name))
        {
            throw new InvalidOperationException($"{type.Name} is not a registered integration event.");
        }

        return new OutboxMessage(
            Guid.NewGuid(), type.Name, JsonSerializer.Serialize(integrationEvent, type, Options), integrationEvent.OccurredAt);
    }

    public static IIntegrationEvent FromOutbox(OutboxMessage message) =>
        Types.TryGetValue(message.Type, out var type)
            && JsonSerializer.Deserialize(message.Payload, type, Options) is IIntegrationEvent integrationEvent
            ? integrationEvent
            : throw new InvalidOperationException($"Outbox message {message.Id} has unknown type {message.Type}.");
}
