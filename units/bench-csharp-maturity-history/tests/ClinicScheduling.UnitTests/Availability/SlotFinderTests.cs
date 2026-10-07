using ClinicScheduling.Domain.Availability;
using Microsoft.Extensions.Time.Testing;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.Availability;

public sealed class SlotFinderTests
{
    private static readonly TimeSpan HalfHour = TimeSpan.FromMinutes(30);
    private readonly FakeTimeProvider _time = new(Now);

    private SlotFinder Finder(params DateOnly[] holidays) => new(new FixedHolidays(holidays), _time);

    private static SlotQuery Tuesday(TimeSpan? duration = null, string? skill = null, bool telehealth = false) =>
        new(Monday.AddDays(1), Monday.AddDays(1), duration ?? HalfHour, skill, telehealth);

    [Fact]
    public void OffersSlotsOnTheGranularityUntilRoomTurnaround()
    {
        var slots = Finder().FindOpenSlots(Schedule(), [], Tuesday(), new SlotSearchOptions());

        Assert.Equal(At(Monday.AddDays(1), 8), slots[0].Start);
        Assert.Equal(At(Monday.AddDays(1), 8, 15), slots[1].Start);
        Assert.Equal(At(Monday.AddDays(1), 15, 45), slots[^1].End);
    }

    [Fact]
    public void SkipsHolidays()
    {
        var slots = Finder(Monday.AddDays(1)).FindOpenSlots(Schedule(), [], Tuesday(), new SlotSearchOptions());

        Assert.Empty(slots);
    }

    [Fact]
    public void KeepsTheBufferAroundBookedAppointments()
    {
        var tuesday = Monday.AddDays(1);
        var booked = new[] { new TimeRange(At(tuesday, 9), At(tuesday, 10)) };

        var slots = Finder().FindOpenSlots(Schedule(), booked, Tuesday(), new SlotSearchOptions());

        Assert.DoesNotContain(slots, s => s.Start > At(tuesday, 8, 25) && s.Start < At(tuesday, 10, 15));
        Assert.Contains(slots, s => s.Start == At(tuesday, 10, 15));
    }

    [Fact]
    public void LeavesTheBreakFree()
    {
        var schedule = Schedule(Weekdays(new TimeOnly(8, 0), new TimeOnly(16, 0), new TimeOnly(12, 0), new TimeOnly(12, 30)));

        var slots = Finder().FindOpenSlots(schedule, [], Tuesday(), new SlotSearchOptions());

        Assert.DoesNotContain(slots, s => s.End > At(Monday.AddDays(1), 12) && s.Start < At(Monday.AddDays(1), 12, 30));
        Assert.Contains(slots, s => s.Start == At(Monday.AddDays(1), 12, 30));
    }

    [Fact]
    public void RespectsMinimumNoticeAndTheSameDayCutoff()
    {
        var today = new SlotQuery(Monday, Monday, HalfHour);

        var morning = Finder().FindOpenSlots(Schedule(), [], today, new SlotSearchOptions());
        _time.SetUtcNow(At(Monday, 14, 5));
        var afternoon = Finder().FindOpenSlots(Schedule(), [], today, new SlotSearchOptions());

        Assert.All(morning, s => Assert.True(s.Start >= Now + TimeSpan.FromHours(2)));
        Assert.Empty(afternoon);
    }

    [Fact]
    public void StopsAtTheDailyCap()
    {
        var tuesday = Monday.AddDays(1);
        var booked = new[] { new TimeRange(At(tuesday, 8), At(tuesday, 8, 30)), new TimeRange(At(tuesday, 9), At(tuesday, 9, 30)) };

        var slots = Finder().FindOpenSlots(Schedule(maxPerDay: 2), booked, Tuesday(), new SlotSearchOptions());

        Assert.Empty(slots);
    }

    [Fact]
    public void MatchesSkillsAndTelehealth()
    {
        var noVideo = Schedule(telehealth: false);
        var inClinicOnly = Schedule(Weekdays(new TimeOnly(8, 0), new TimeOnly(16, 0)).Select(h => h with { InClinicOnly = true }));

        Assert.Empty(Finder().FindOpenSlots(Schedule(), [], Tuesday(skill: "pelvic-floor"), new SlotSearchOptions()));
        Assert.NotEmpty(Finder().FindOpenSlots(Schedule(), [], Tuesday(skill: "SPORTS"), new SlotSearchOptions()));
        Assert.Empty(Finder().FindOpenSlots(noVideo, [], Tuesday(telehealth: true), new SlotSearchOptions()));
        Assert.Empty(Finder().FindOpenSlots(inClinicOnly, [], Tuesday(telehealth: true), new SlotSearchOptions()));
    }

    [Fact]
    public void VideoVisitsNeedNoRoomTurnaround()
    {
        var slots = Finder().FindOpenSlots(Schedule(), [], Tuesday(telehealth: true), new SlotSearchOptions());

        Assert.Equal(At(Monday.AddDays(1), 16), slots[^1].End);
        Assert.All(slots, s => Assert.True(s.IsTelehealth));
    }

    [Fact]
    public void NeverLooksFurtherAheadThanAllowed()
    {
        var query = new SlotQuery(Monday.AddDays(1), Monday.AddDays(30), HalfHour);

        var slots = Finder().FindOpenSlots(Schedule(), [], query, new SlotSearchOptions { MaxDaysAhead = 2, MaxResults = 1000 });

        Assert.All(slots, s => Assert.True(s.Start < At(Monday.AddDays(3), 0)));
    }

    [Fact]
    public void RejectsEmptyQueries()
    {
        Assert.Empty(Finder().FindOpenSlots(Schedule(), [], new SlotQuery(Monday.AddDays(2), Monday.AddDays(1), HalfHour), new SlotSearchOptions()));
        Assert.Empty(Finder().FindOpenSlots(Schedule(), [], Tuesday(TimeSpan.Zero), new SlotSearchOptions()));
    }
}
