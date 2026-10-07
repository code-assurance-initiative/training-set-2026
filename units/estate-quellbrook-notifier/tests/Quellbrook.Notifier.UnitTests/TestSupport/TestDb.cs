using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Quellbrook.Notifier.Channels;
using Quellbrook.Notifier.Notifications;
using Quellbrook.Notifier.Persistence;

namespace Quellbrook.Notifier.UnitTests.TestSupport;

/// <summary>An in-memory SQLite database and the notifier's handlers over it, with recording channels.</summary>
internal sealed class TestDb : IDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 8, 3, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestDb()
    {
        _connection.Open();
        using (var db = Context())
        {
            db.Database.EnsureCreated();
        }

        Services = new ServiceCollection()
            .AddScoped(_ => Context())
            .AddSingleton<TimeProvider>(Time)
            .AddSingleton<IEmailSender>(Email)
            .AddSingleton<ISmsSender>(Sms)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddScoped<NotificationService>()
            .AddScoped<OrderPlacedHandler>()
            .AddScoped<DeliveryHandlers>()
            .BuildServiceProvider();
    }

    public FakeTimeProvider Time { get; } = new(Now);

    public RecordingEmailSender Email { get; } = new();

    public RecordingSmsSender Sms { get; } = new();

    public ServiceProvider Services { get; }

    public NotifierDbContext Context() =>
        new(new DbContextOptionsBuilder<NotifierDbContext>()
            .UseSqlite(_connection)
            .ReplaceService<IModelCustomizer, SqliteModelCustomizer>()
            .Options);

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }

    private sealed class SqliteModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
            {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}

internal sealed class RecordingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public bool Fail { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (Fail)
        {
            throw new ProviderException("provider unavailable");
        }

        Sent.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingSmsSender : ISmsSender
{
    public List<SmsMessage> Sent { get; } = [];

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
