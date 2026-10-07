using System.Net.Http.Json;

namespace FleetOps.Infrastructure.Tyres;

public sealed class TyreVendorClient(HttpClient http) : ITyreVendorClient
{
    public async Task<IReadOnlyList<TyreQuote>> QuoteAsync(TyreQuoteRequest request, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(new Uri("quotes", UriKind.Relative), request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TyreQuote>>(cancellationToken).ConfigureAwait(false) ?? [];
    }

    public async Task<TyreOrder> OrderAsync(TyreOrderRequest request, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(new Uri("orders", UriKind.Relative), request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TyreOrder>(cancellationToken).ConfigureAwait(false)
            ?? throw new HttpRequestException("The tyre vendor returned no order.");
    }
}
