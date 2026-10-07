using Shipping.Rates.Core.Accounts;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Accounts;

public sealed class CarrierAccountManagerTests
{
    private readonly CarrierAccountManager _accounts = new(requestsPerMinute: 2, auditCapacity: 3, webhookSecret: [7, 7, 7, 7]);

    [Fact]
    public void Keys_are_stored_and_age()
    {
        _accounts.RotateKey("ALDER", "k1", TestData.Now);

        Assert.Equal("k1", _accounts.KeyFor("ALDER"));
        Assert.Null(_accounts.KeyFor("CORVID"));
        Assert.False(_accounts.IsKeyStale("ALDER", TestData.Now.AddDays(1), TimeSpan.FromDays(30)));
        Assert.True(_accounts.IsKeyStale("ALDER", TestData.Now.AddDays(31), TimeSpan.FromDays(30)));
    }

    [Fact]
    public void Requests_are_limited_per_minute()
    {
        Assert.True(_accounts.TryConsumeRequest("ALDER", TestData.Now));
        Assert.True(_accounts.TryConsumeRequest("ALDER", TestData.Now));
        Assert.False(_accounts.TryConsumeRequest("ALDER", TestData.Now.AddSeconds(30)));
        Assert.Equal(2, _accounts.RequestsUsed("ALDER"));
        Assert.True(_accounts.TryConsumeRequest("ALDER", TestData.Now.AddMinutes(1)));
    }

    [Fact]
    public void The_audit_trail_keeps_the_latest_entries()
    {
        for (var i = 0; i < 5; i++)
        {
            _accounts.RecordAudit($"entry {i}", TestData.Now);
        }

        var recent = _accounts.RecentAudit(10);
        Assert.Equal(3, recent.Count);
        Assert.EndsWith("entry 4", recent[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void Webhook_signatures_round_trip_and_reject_tampering()
    {
        var signature = _accounts.SignWebhook("{\"a\":1}");

        Assert.StartsWith("sha256=", signature, StringComparison.Ordinal);
        Assert.True(_accounts.VerifyWebhook("{\"a\":1}", signature));
        Assert.False(_accounts.VerifyWebhook("{\"a\":2}", signature));
        Assert.False(_accounts.VerifyWebhook("{\"a\":1}", null));
    }
}
