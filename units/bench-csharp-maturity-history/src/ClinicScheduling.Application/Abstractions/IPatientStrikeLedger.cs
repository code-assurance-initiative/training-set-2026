namespace ClinicScheduling.Application.Abstractions;

/// <summary>When each patient was charged a strike under the cancellation policy.</summary>
public interface IPatientStrikeLedger
{
    Task RecordAsync(Guid patientId, DateTimeOffset at, CancellationToken cancellationToken);

    Task<IReadOnlyList<DateTimeOffset>> StrikesAsync(Guid patientId, CancellationToken cancellationToken);
}
