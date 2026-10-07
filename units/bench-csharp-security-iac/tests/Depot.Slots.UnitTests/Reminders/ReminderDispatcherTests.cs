using Depot.Slots.Core.Bookings;
using Depot.Slots.Core.Reminders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Depot.Slots.UnitTests.Reminders;

public sealed class ReminderDispatcherTests
{
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly InMemoryBookingStore _store = new();
    private readonly RecordingSender _sender = new();
    private readonly ReminderDispatcher _dispatcher;

    public ReminderDispatcherTests()
    {
        var options = Options.Create(new ReminderOptions { LeadTime = TimeSpan.FromMinutes(30) });
        _dispatcher = new ReminderDispatcher(_store, _sender, options, _clock, NullLogger<ReminderDispatcher>.Instance);
    }

    private async Task<Booking> AddAsync(TimeSpan startsIn, int minutes = 30)
    {
        var start = _clock.GetUtcNow() + startsIn;
        var booking = new Booking(Guid.NewGuid(), "D01", $"CARR-{_sender.Sent.Count}", start, start.AddMinutes(minutes), null);
        Assert.True(await _store.TryAddAsync(booking, TestContext.Current.CancellationToken));
        return booking;
    }

    [Fact]
    public async Task AnnouncesEachBookingInsideTheLeadTimeExactlyOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var due = await AddAsync(TimeSpan.FromMinutes(20));
        await AddAsync(TimeSpan.FromHours(2));

        Assert.Equal(1, await _dispatcher.DispatchDueAsync(ct));
        Assert.Equal(0, await _dispatcher.DispatchDueAsync(ct));
        Assert.Equal([due.Id], _sender.Sent);
    }

    [Fact]
    public async Task AFailedSendIsRetriedOnTheNextPass()
    {
        var ct = TestContext.Current.CancellationToken;
        var due = await AddAsync(TimeSpan.FromMinutes(10));
        _sender.FailNext = true;

        Assert.Equal(0, await _dispatcher.DispatchDueAsync(ct));
        Assert.Equal(1, await _dispatcher.DispatchDueAsync(ct));
        Assert.Equal([due.Id], _sender.Sent);
    }

    [Fact]
    public async Task ABookingThatHasAlreadyEndedIsNotAnnounced()
    {
        var ct = TestContext.Current.CancellationToken;
        await AddAsync(TimeSpan.FromMinutes(5), minutes: 15);
        _clock.Advance(TimeSpan.FromMinutes(25));

        Assert.Equal(0, await _dispatcher.DispatchDueAsync(ct));
        Assert.Empty(_sender.Sent);
    }

    private sealed class RecordingSender : IReminderSender
    {
        public List<Guid> Sent { get; } = [];

        public bool FailNext { get; set; }

        public Task SendAsync(Booking booking, CancellationToken cancellationToken)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new HttpRequestException("chat service unavailable");
            }

            Sent.Add(booking.Id);
            return Task.CompletedTask;
        }
    }
}
