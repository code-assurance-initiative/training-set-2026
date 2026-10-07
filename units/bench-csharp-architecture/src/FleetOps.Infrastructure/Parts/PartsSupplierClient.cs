using System.Net.Http.Json;

namespace FleetOps.Infrastructure.Parts;

public sealed class PartsSupplierClient(HttpClient http) : IPartsSupplierClient
{
    public async Task<IReadOnlyList<Part>> GetCatalogueAsync(CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<List<Part>>(new Uri("catalogue", UriKind.Relative), cancellationToken).ConfigureAwait(false) ?? [];

    public async Task<PartQuote> QuoteAsync(PartNumber number, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(number);
        return await http.GetFromJsonAsync<PartQuote>(new Uri($"parts/{Uri.EscapeDataString(number.Value)}/quote", UriKind.Relative), cancellationToken)
                .ConfigureAwait(false)
            ?? throw new PartsSupplierException($"No quote for part {number}.");
    }

    public async Task<PartOrder> OrderAsync(IReadOnlyList<PartOrderLine> lines, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(new Uri("orders", UriKind.Relative), lines, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PartOrder>(cancellationToken).ConfigureAwait(false)
            ?? throw new PartsSupplierException("The supplier returned no order.");
    }
}
