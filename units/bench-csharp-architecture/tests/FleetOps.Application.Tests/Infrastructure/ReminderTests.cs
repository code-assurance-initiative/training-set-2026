using FleetOps.Application.Tests.Support;
using FleetOps.Contracts.Maintenance;
using FleetOps.Domain.Common;
using FleetOps.Domain.Maintenance;
using FleetOps.Domain.Vehicles;
using FleetOps.Infrastructure.Email;
using FleetOps.Infrastructure.Persistence;
using FleetOps.Infrastructure.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FleetOps.Application.Tests.Infrastructure;

public sealed class ReminderTests : IDisposable
{
    private static readonly QuietHours Night = new(new TimeOnly(20, 0), new TimeOnly(7, 0));
    private readonly string _pickup = Path.Combine(Path.GetTempPath(), "fleetops-mail-" + Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static MaintenanceDue Due => new(Guid.NewGuid(), "AB12345", "Oil and filters", 15_000, 15_200);

    [Theory]
    [InlineData("workshop@mail.fleet.internal", true)]
    [InlineData("workshop", false)]
    [InlineData("@fleet", false)]
    public void EmailAddressesNeedALocalPartAndADomain(string value, bool valid)
    {
        if (valid)
        {
            Assert.Equal(value, new EmailAddress(value).ToString());
        }
        else
        {
            Assert.Throws<ArgumentException>(() => new EmailAddress(value));
        }
    }

    [Fact]
    public async Task ARemindersIsWrittenToThePickupDirectoryOutsideQuietHours()
    {
        var mailer = new ReminderMailer(Sender(), Night, _clock);

        Assert.True(await mailer.SendAsync(Due, new EmailAddress("workshop@mail.fleet.internal"), Ct));

        var file = Assert.Single(Directory.GetFiles(_pickup, "*.eml"));
        var text = await File.ReadAllTextAsync(file, Ct);
        Assert.Contains("Subject: AB12345: Oil and filters is due", text, StringComparison.Ordinal);
        Assert.Contains("15,200 km", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemindersAreHeldBackDuringQuietHours()
    {
        _clock.SetUtcNow(new DateTimeOffset(2026, 5, 4, 22, 0, 0, TimeSpan.Zero));
        var dispatcher = new ReminderDispatcher(new ReminderMailer(Sender(), Night, _clock), Scheduling(), _clock);

        var run = await dispatcher.DispatchAsync([Due, Due], Ct);

        Assert.Equal(new ReminderRun(_clock.GetUtcNow(), 2, 0, 2), run);
        Assert.False(Directory.Exists(_pickup));
    }

    [Fact]
    public async Task AFailedWriteIsADeliveryError()
    {
        Directory.CreateDirectory(_pickup);
        var blocked = Path.Combine(_pickup, "not-a-directory");
        await File.WriteAllTextAsync(blocked, string.Empty, Ct);
        var sender = new PickupDirectoryEmailSender(
            Options.Create(new EmailOptions { PickupDirectory = blocked, From = "fleetops@mail.fleet.internal" }), _clock, NullLogger<PickupDirectoryEmailSender>.Instance);

        await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            sender.SendAsync(new EmailMessage(new EmailAddress("a@b.c"), "s", "b", EmailPriority.High, []), Ct));
    }

    [Fact]
    public async Task ASchedulerRunAsksWhatIsDueAndMailsIt()
    {
        using var database = new TestDatabase();
        await using (var seed = database.CreateContext())
        {
            seed.Vehicles.Add(Vehicle.Register(new Vin("WVWZZZ1KZAW000001"), "AB12345", "Transit", 14_000));
            await seed.SaveChangesAsync(Ct);
        }

        var services = new ServiceCollection();
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddScoped(_ => database.CreateContext());
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<FleetOpsDbContext>());
        services.AddScoped(_ => new MaintenanceDueEvaluator(new FakeTelematics { Odometers = { ["WVWZZZ1KZAW000001"] = 15_100 } }));
        services.AddSingleton<IEmailSender>(Sender());
        services.AddSingleton(Night);
        services.AddSingleton(Scheduling());
        services.AddScoped<ReminderMailer>();
        services.AddScoped<ReminderDispatcher>();
        await using var provider = services.BuildServiceProvider();
        var scheduler = new ReminderScheduler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new MaintenanceReminderPlanner(new ReminderWindowCalculator()),
            Night,
            _clock,
            NullLogger<ReminderScheduler>.Instance);

        var run = await scheduler.RunOnceAsync(Ct);

        Assert.Equal(1, run.Sent);
        Assert.Single(Directory.GetFiles(_pickup, "*.eml"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_pickup))
        {
            Directory.Delete(_pickup, recursive: true);
        }
    }

    private static IOptions<SchedulingOptions> Scheduling() =>
        Options.Create(new SchedulingOptions { ReminderRecipient = "workshop@mail.fleet.internal" });

    private PickupDirectoryEmailSender Sender() =>
        new PickupDirectoryEmailSender(
            Options.Create(new EmailOptions { PickupDirectory = _pickup, From = "fleetops@mail.fleet.internal" }), _clock, NullLogger<PickupDirectoryEmailSender>.Instance);
}
