using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Application.Abstractions;

public interface IScheduleRepository
{
    Task<PractitionerSchedule?> FindAsync(Guid practitionerId, CancellationToken cancellationToken);

    Task UpsertAsync(PractitionerSchedule schedule, CancellationToken cancellationToken);
}
