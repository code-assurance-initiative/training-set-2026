using FleetOps.Infrastructure.Documents;
using FleetOps.Infrastructure.FuelCards;
using FleetOps.Infrastructure.Parts;
using FleetOps.Infrastructure.Tyres;
using Microsoft.Extensions.Options;

namespace FleetOps.Application.Tests.Infrastructure;

public sealed class DocumentAndVendorTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("=SUM(A1)", "'=SUM(A1)")]
    public void CsvFieldsAreQuotedAndFormulasDefused(string value, string expected)
    {
        Assert.Equal(expected, CsvEscaping.Field(value, ','));
    }

    [Fact]
    public void TyreSizesParseIsoNotation()
    {
        Assert.Equal(new TyreSize(205, 55, 16), TyreSize.Parse("205/55R16"));
        Assert.Equal("225/45R17", TyreSize.Parse("225/45 r17").ToString());
        Assert.Throws<FormatException>(() => TyreSize.Parse("205-55-16"));
    }

    [Fact]
    public void PartNumbersCompareWithoutCase()
    {
        Assert.Equal(new PartNumber("bp-1234"), new PartNumber("BP-1234"));
        Assert.Throws<ArgumentException>(() => new PartNumber("BP 1234"));
    }

    [Fact]
    public async Task TheFuelCardClientRetriesATransientFailure()
    {
        var inner = new FlakyFuelCardClient(failures: 1);
        var client = new RetryingFuelCardClient(inner, Options.Create(new FuelCardOptions { MaxAttempts = 3 }), TimeProvider.System);

        var account = await client.GetAccountAsync(TestContext.Current.CancellationToken);

        Assert.Equal("FC-1", account.AccountNumber);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task TheFuelCardClientGivesUpAfterTheLastAttempt()
    {
        var inner = new FlakyFuelCardClient(failures: 5);
        var client = new RetryingFuelCardClient(inner, Options.Create(new FuelCardOptions { MaxAttempts = 1 }), TimeProvider.System);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAccountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, inner.Calls);
    }

    private sealed class FlakyFuelCardClient(int failures) : IFuelCardClient
    {
        public int Calls { get; private set; }

        public Task<FuelCardTransactionPage> GetTransactionsAsync(string? cursor, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FuelCardAccount> GetAccountAsync(CancellationToken cancellationToken) =>
            ++Calls <= failures
                ? throw new HttpRequestException("503")
                : Task.FromResult(new FuelCardAccount("FC-1", 12, 5_000m, FuelCardStatus.Active));
    }
}
