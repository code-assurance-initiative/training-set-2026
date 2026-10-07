using System.Net;
using System.Net.Http.Json;

namespace Warehouse.Stock.IntegrationTests;

public static class ApiClientExtensions
{
    public const string SkuCode = "BOLT-M8-40";
    public const string BinCode = "A01-02-03";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Registers a SKU and a bin and receives <paramref name="onHand"/> units into it.</summary>
    public static async Task SeedStockAsync(this HttpClient client, int onHand)
    {
        await client.PostOkAsync("api/skus", new { code = SkuCode, description = "Hex bolt M8 x 40", unitOfMeasure = "EA" });
        await client.PostOkAsync("api/bins", new { code = BinCode, zone = "BULK", capacity = 500 });
        await client.PostOkAsync($"api/stock/{SkuCode}/receipts", new { binCode = BinCode, quantity = onHand });
    }

    public static async Task<HttpResponseMessage> PostOkAsync(this HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body, Token);
        Assert.True(
            response.IsSuccessStatusCode,
            $"POST {path} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(Token)}");
        return response;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(Token);
        Assert.NotNull(body);
        return body;
    }

    public static async Task<HttpStatusCode> StatusOfPostAsync(this HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body, Token);
        return response.StatusCode;
    }
}
