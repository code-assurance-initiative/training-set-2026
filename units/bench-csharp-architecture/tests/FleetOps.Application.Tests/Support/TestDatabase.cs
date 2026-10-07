using FleetOps.Infrastructure.Auditing;
using FleetOps.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace FleetOps.Application.Tests.Support;

/// <summary>A private in-memory SQLite database with the real schema, kept alive for one test.</summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.Migrate();
    }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero));

    public FleetOpsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FleetOpsDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditSaveChangesInterceptor(Clock))
            .Options);

    public void Dispose() => _connection.Dispose();
}
