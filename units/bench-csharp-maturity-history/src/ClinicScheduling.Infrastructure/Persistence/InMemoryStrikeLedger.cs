using System.Collections.Concurrent;
using System.Collections.Immutable;
using ClinicScheduling.Application.Abstractions;

namespace ClinicScheduling.Infrastructure.Persistence;

public sealed class InMemoryStrikeLedger : IPatientStrikeLedger
{
    private readonly ConcurrentDictionary<Guid, ImmutableList<DateTimeOffset>> _strikes = new();

    public Task RecordAsync(Guid patientId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        _strikes.AddOrUpdate(patientId, _ => [at], (_, existing) => existing.Add(at));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DateTimeOffset>> StrikesAsync(Guid patientId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DateTimeOffset>>(_strikes.GetValueOrDefault(patientId, []));
}
