using System.Net.Http.Json;

namespace FleetOps.Infrastructure.FuelCards;

public sealed class FuelCardClient(HttpClient http) : IFuelCardClient
{
    public async Task<FuelCardTransactionPage> GetTransactionsAsync(string? cursor, CancellationToken cancellationToken)
    {
        var path = cursor is null ? "transactions" : $"transactions?cursor={Uri.EscapeDataString(cursor)}";
        return await http.GetFromJsonAsync<FuelCardTransactionPage>(new Uri(path, UriKind.Relative), cancellationToken).ConfigureAwait(false)
            ?? throw new FuelCardApiException("The fuel-card API returned an empty transaction page.");
    }

    public async Task<FuelCardAccount> GetAccountAsync(CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<FuelCardAccount>(new Uri("account", UriKind.Relative), cancellationToken).ConfigureAwait(false)
            ?? throw new FuelCardApiException("The fuel-card API returned no account.");
}
