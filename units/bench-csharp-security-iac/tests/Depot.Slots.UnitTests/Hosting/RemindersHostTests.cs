using System.Net;
using Depot.Slots.Core.Bookings;
using Depot.Slots.Core.Reminders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Depot.Slots.UnitTests.Hosting;

/// <summary>Boots the real reminder worker host over the in-memory store, with the chat sender replaced.</summary>
public sealed class RemindersHostTests
{
    [Fact]
    public async Task TheWorkerAnnouncesADueBookingAndTheHostReportsReady()
    {
        var ct = TestContext.Current.CancellationToken;
        var sender = new SignallingSender();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Storage:Provider", "InMemory");
            builder.UseSetting("Reminders:PollInterval", "00:00:10");
            builder.UseSetting("Chat:Channel", "#yard-arrivals-test");
            builder.UseSetting("Chat:BotToken", Guid.NewGuid().ToString("N"));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IReminderSender>();
                services.AddSingleton<IReminderSender>(sender);
            });
        });

        var store = factory.Services.GetRequiredService<IBookingStore>();
        var start = DateTimeOffset.UtcNow.AddMinutes(10);
        var booking = new Booking(Guid.NewGuid(), "D02", "CARR-HOST", start, start.AddMinutes(30), null);
        await store.TryAddAsync(booking, ct);

        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", ct)).StatusCode);

        var announced = await sender.Announced.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        Assert.Equal(booking.Id, announced);

        var ready = await client.GetAsync("/health/ready", ct);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    private sealed class SignallingSender : IReminderSender
    {
        public TaskCompletionSource<Guid> Announced { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(Booking booking, CancellationToken cancellationToken)
        {
            Announced.TrySetResult(booking.Id);
            return Task.CompletedTask;
        }
    }
}
