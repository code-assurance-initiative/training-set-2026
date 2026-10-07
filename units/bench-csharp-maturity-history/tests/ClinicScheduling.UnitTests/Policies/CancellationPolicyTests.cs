using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Domain.Policies;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.Policies;

public sealed class CancellationPolicyTests
{
    private static readonly DateTimeOffset Start = At(Monday.AddDays(3), 10);
    private readonly CancellationPolicy _policy = new(new CancellationPolicyOptions());

    private static Appointment Booked(bool telehealth = false) =>
        Appointment.Book(Guid.NewGuid(), PractitionerId, Guid.NewGuid(), new TimeRange(Start, Start.AddMinutes(45)), telehealth);

    [Fact]
    public void CancellingInGoodTimeIsFree()
    {
        var decision = _policy.EvaluateCancellation(Booked(), CancellationReason.PatientRequest, Start.AddHours(-25));

        Assert.Equal(CancellationDecision.Free(CancellationPolicy.FreeCancellation), decision);
    }

    [Fact]
    public void LateCancellationCostsAFeeAndAStrike()
    {
        var decision = _policy.EvaluateCancellation(Booked(), CancellationReason.PatientRequest, Start.AddHours(-3));

        Assert.Equal(new CancellationDecision(250m, true, CancellationPolicy.LateCancellation), decision);
    }

    [Fact]
    public void LateVideoCancellationCostsHalf()
    {
        var decision = _policy.EvaluateCancellation(Booked(telehealth: true), CancellationReason.PatientRequest, Start.AddHours(-3));

        Assert.Equal(125m, decision.Fee);
    }

    [Fact]
    public void IllnessIsFreeButStillAStrike()
    {
        var decision = _policy.EvaluateCancellation(Booked(), CancellationReason.PatientIllness, Start.AddHours(-1));

        Assert.Equal(0m, decision.Fee);
        Assert.True(decision.CountsAsStrike);
        Assert.Equal(CancellationPolicy.Illness, decision.Code);
    }

    [Theory]
    [InlineData(CancellationReason.PractitionerUnavailable)]
    [InlineData(CancellationReason.ClinicClosed)]
    public void ClinicInitiatedCancellationsNeverCostThePatient(CancellationReason reason)
    {
        var decision = _policy.EvaluateCancellation(Booked(), reason, Start.AddMinutes(-5));

        Assert.Equal(CancellationDecision.Free(CancellationPolicy.ClinicInitiated), decision);
    }

    [Fact]
    public void NoShowCostsTheFullNoShowFee()
    {
        Assert.Equal(new CancellationDecision(400m, true, CancellationPolicy.NoShow), _policy.EvaluateNoShow(Booked()));
    }

    [Fact]
    public void TwoRecentStrikesRequireADeposit()
    {
        var now = Now;

        Assert.False(_policy.RequiresDeposit([now.AddDays(-10)], now));
        Assert.True(_policy.RequiresDeposit([now.AddDays(-10), now.AddDays(-100)], now));
        Assert.False(_policy.RequiresDeposit([now.AddDays(-10), now.AddDays(-200)], now));
    }

    [Fact]
    public void TheFreeCancellationDeadlineIsADayBefore()
    {
        Assert.Equal(Start.AddHours(-24), _policy.FreeCancellationDeadline(Booked()));
    }
}
