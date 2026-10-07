using FluentAssertions;
using Invoicing.Worker.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;

namespace Invoicing.UnitTests.Worker;

/// <summary>
/// The store's SQL needs a MySQL server, which the offline test suite does not have; what can be checked without one
/// is that an unreachable database surfaces as a <see cref="MySqlException"/> the processor treats as a job failure.
/// </summary>
public sealed class MySqlRenderJobStoreTests
{
    private const string UnreachableDatabase = "Server=127.0.0.1;Port=9;Database=billing;Connection Timeout=2;Pooling=false";

    [Fact]
    public async Task AnUnreachableDatabaseFailsTheClaim()
    {
        var store = new MySqlRenderJobStore(UnreachableDatabase, "worker-1", NullLogger<MySqlRenderJobStore>.Instance);

        var act = () => store.ClaimAsync(10, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<MySqlException>();
    }

    [Fact]
    public async Task AnUnreachableDatabaseFailsTheOutcomeUpdate()
    {
        var store = new MySqlRenderJobStore(UnreachableDatabase, "worker-1", NullLogger<MySqlRenderJobStore>.Instance);

        var act = () => store.MarkFailedAsync(42, new string('x', 600), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<MySqlException>();
    }
}
