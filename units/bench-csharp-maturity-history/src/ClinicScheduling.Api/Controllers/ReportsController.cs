using ClinicScheduling.Api.Security;
using ClinicScheduling.Application.Abstractions;
using ClinicScheduling.Domain.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicScheduling.Api.Controllers;

public sealed record NoShowReportRow(Guid PractitionerId, int Appointments, int NoShows, int LateCancellations);

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.ReportsRead)]
public sealed class ReportsController(IAppointmentRepository appointments) : ControllerBase
{
    /// <summary>No-shows and cancellations per practitioner for appointments starting in [from, to).</summary>
    [HttpGet("no-shows")]
    public async Task<ActionResult<IReadOnlyList<NoShowReportRow>>> NoShowsAsync(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        if (to <= from || to.DayNumber - from.DayNumber > 366)
        {
            return ValidationProblem("The range must be between one day and one year.");
        }

        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var inRange = await appointments.StartingBetweenAsync(start, end, cancellationToken).ConfigureAwait(false);
        return Ok(inRange
            .GroupBy(a => a.PractitionerId)
            .Select(g => new NoShowReportRow(
                g.Key,
                g.Count(),
                g.Count(a => a.Status == AppointmentStatus.NoShow),
                g.Count(a => a.Status == AppointmentStatus.Cancelled && a.CancelledAt > a.Time.Start.AddHours(-24))))
            .OrderByDescending(r => r.NoShows)
            .ToList());
    }
}
