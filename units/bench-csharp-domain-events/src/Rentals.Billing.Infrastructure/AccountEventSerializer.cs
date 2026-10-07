using System.Text.Json;
using System.Text.Json.Nodes;
using Rentals.Billing.Domain.Accounts.Events;
using Rentals.SharedKernel;

namespace Rentals.Billing.Infrastructure;

/// <summary>An event as the store keeps it: a stable type name, the schema version it was written with, and its JSON.</summary>
public sealed record SerializedEvent(string EventType, int SchemaVersion, string Data);

/// <summary>
/// Writes member-account events to their stored form and reads them back. Stored events are never rewritten, so the
/// JSON shape of an event type is a contract with every event of that type already in the store: a change to the shape
/// gets a new schema version and an upcaster that lifts the older JSON to the next version before it is deserialised.
/// </summary>
public static class AccountEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static readonly EventType[] Types =
    [
        new(typeof(MemberAccountOpened), "member-account-opened", 1),
        new(typeof(ChargePosted), "charge-posted", 1),
        new(typeof(DepositHeld), "deposit-held", 1),
        new(typeof(DepositReleased), "deposit-released", 1),
        new(typeof(LateFeeCharged), "late-fee-charged", 2),
        new(typeof(PaymentReceived), "payment-received", 1),
    ];

    private static readonly Dictionary<(string EventType, int FromVersion), Func<JsonObject, JsonObject>> Upcasters = new()
    {
        [("late-fee-charged", 1)] = LateFeeChargedV1ToV2,
    };

    public static SerializedEvent Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var type = Array.Find(Types, t => t.ClrType == domainEvent.GetType())
            ?? throw new InvalidOperationException($"{domainEvent.GetType().Name} is not a member-account event.");
        return new SerializedEvent(type.Name, type.Version, JsonSerializer.Serialize(domainEvent, type.ClrType, Options));
    }

    public static IDomainEvent Deserialize(SerializedEvent stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        var type = Array.Find(Types, t => string.Equals(t.Name, stored.EventType, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Unknown stored event type '{stored.EventType}'.");
        if (stored.SchemaVersion > type.Version)
        {
            throw new InvalidOperationException(
                $"{stored.EventType} v{stored.SchemaVersion} is newer than this reader (v{type.Version}).");
        }

        var json = JsonNode.Parse(stored.Data)?.AsObject()
            ?? throw new InvalidOperationException($"Stored {stored.EventType} is empty.");
        for (var version = stored.SchemaVersion; version < type.Version; version++)
        {
            var upcast = Upcasters.GetValueOrDefault((type.Name, version))
                ?? throw new InvalidOperationException($"No upcaster for {type.Name} v{version}.");
            json = upcast(json);
        }

        return (IDomainEvent)(json.Deserialize(type.ClrType, Options)
            ?? throw new InvalidOperationException($"Stored {stored.EventType} is empty."));
    }

    /// <summary>Version 1 stored the total fee only; version 2 stores the daily fee it was computed from.</summary>
    private static JsonObject LateFeeChargedV1ToV2(JsonObject v1)
    {
        var total = v1["amount"]?.Deserialize<Money>(Options)
            ?? throw new InvalidOperationException("A version-1 late fee has no amount.");
        var daysLate = v1["daysLate"]?.GetValue<int>()
            ?? throw new InvalidOperationException("A version-1 late fee has no day count.");
        v1.Remove("amount");
        v1["dailyFee"] = JsonSerializer.SerializeToNode(new Money(total.Amount / daysLate, total.Currency), Options);
        return v1;
    }

    private sealed record EventType(Type ClrType, string Name, int Version);
}
