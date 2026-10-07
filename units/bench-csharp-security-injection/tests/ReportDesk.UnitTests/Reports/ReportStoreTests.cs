using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReportDesk.Api.Reports;

namespace ReportDesk.UnitTests.Reports;

/// <summary>The EF Core stores against an in-memory SQLite database (the SQL they use is portable).</summary>
public sealed class ReportStoreTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ReportsDbContext _db;

    public ReportStoreTests()
    {
        _connection.Open();
        _db = new ReportsDbContext(new DbContextOptionsBuilder<ReportsDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Reports.AddRange(
            new ReportDefinition { Id = Guid.NewGuid(), Name = "Weekly", Owner = "k.holm" },
            new ReportDefinition { Id = Guid.NewGuid(), Name = "Archive", Owner = "k.holm", Archived = true },
            new ReportDefinition { Id = Guid.NewGuid(), Name = "Audit", Owner = "a.berg" });
        _db.Subscribers.Add(new Subscriber { Id = SubscriberId, EmailAddress = "ann.berg@archive.test" });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    private static Guid SubscriberId { get; } = Guid.NewGuid();

    [Fact]
    public async Task ListsAnOwnersActiveReportsByName()
    {
        var reports = await new ReportRepository(_db).ForOwnerAsync("k.holm", TestContext.Current.CancellationToken);
        Assert.Equal(["Weekly"], reports.Select(report => report.Name));
    }

    [Fact]
    public async Task SavesALayout()
    {
        var repository = new ReportRepository(_db);
        var weekly = (await repository.ForOwnerAsync("k.holm", TestContext.Current.CancellationToken))[0];

        await repository.SaveLayoutAsync(weekly.Id, """{"PageSize":"A3"}""", TestContext.Current.CancellationToken);

        var saved = await repository.GetAsync(weekly.Id, TestContext.Current.CancellationToken);
        Assert.Equal("""{"PageSize":"A3"}""", saved?.LayoutJson);
    }

    [Fact]
    public async Task SwitchesASubscribersEmailChannel()
    {
        var store = new SubscriberStore(_db);
        await store.SetEmailEnabledAsync(SubscriberId, enabled: false, TestContext.Current.CancellationToken);

        var subscriber = await store.GetAsync(SubscriberId, TestContext.Current.CancellationToken);
        Assert.False(subscriber?.EmailEnabled);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
