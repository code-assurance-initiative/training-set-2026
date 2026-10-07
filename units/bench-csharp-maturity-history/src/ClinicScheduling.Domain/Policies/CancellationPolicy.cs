using ClinicScheduling.Domain.Appointments;

namespace ClinicScheduling.Domain.Policies;

/// <summary>
/// The clinic's cancellation and no-show rules: who pays a fee, how much, and which events count as strikes that
/// eventually require a deposit before the patient can book again.
/// </summary>
public sealed class CancellationPolicy(CancellationPolicyOptions options)
{
    public const string FreeCancellation = "free";
    public const string LateCancellation = "late";
    public const string NoShow = "no-show";
    public const string ClinicInitiated = "clinic-initiated";
    public const string Illness = "illness";

    /// <summary>The consequence of cancelling <paramref name="appointment"/> at <paramref name="at"/>.</summary>
    public CancellationDecision EvaluateCancellation(Appointment appointment, CancellationReason reason, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        if (IsClinicInitiated(reason))
        {
            return CancellationDecision.Free(ClinicInitiated);
        }

        if (!IsLate(appointment, at))
        {
            return CancellationDecision.Free(FreeCancellation);
        }

        if (reason == CancellationReason.PatientIllness)
        {
            return CancellationDecision.Free(Illness) with { CountsAsStrike = true };
        }

        return new CancellationDecision(Scaled(options.LateCancellationFee, appointment), true, LateCancellation);
    }

    /// <summary>The consequence of the patient not turning up at all.</summary>
    public CancellationDecision EvaluateNoShow(Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        return new CancellationDecision(Scaled(options.NoShowFee, appointment), true, NoShow);
    }

    /// <summary>Whether a patient with these past strikes must pay a deposit to book at <paramref name="now"/>.</summary>
    public bool RequiresDeposit(IEnumerable<DateTimeOffset> strikes, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(strikes);
        var since = now - options.StrikeWindow;
        return strikes.Count(s => s >= since) >= options.StrikesBeforeDeposit;
    }

    /// <summary>The latest moment the appointment can still be cancelled free of charge.</summary>
    public DateTimeOffset FreeCancellationDeadline(Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        return appointment.Time.Start - options.FreeCancellationWindow;
    }

    private bool IsLate(Appointment appointment, DateTimeOffset at) => at > FreeCancellationDeadline(appointment);

    private static bool IsClinicInitiated(CancellationReason reason) =>
        reason is CancellationReason.PractitionerUnavailable or CancellationReason.ClinicClosed;

    private decimal Scaled(decimal fee, Appointment appointment) =>
        appointment.IsTelehealth ? decimal.Round(fee * options.TelehealthFeeFactor, 2) : fee;
}
