using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.FuelCards;

/// <summary>Retries transient failures of the fuel-card API with exponential back-off (200 ms, 400 ms, …).</summary>
public sealed class RetryingFuelCardClient(IFuelCardClient inner, IOptions<FuelCardOptions> options, TimeProvider clock) : IFuelCardClient
{
    public Task<FuelCardTransactionPage> GetTransactionsAsync(string? cursor, CancellationToken cancellationToken) =>
        WithRetryAsync(ct => inner.GetTransactionsAsync(cursor, ct), cancellationToken);

    public Task<FuelCardAccount> GetAccountAsync(CancellationToken cancellationToken) =>
        WithRetryAsync(inner.GetAccountAsync, cancellationToken);

    private async Task<T> WithRetryAsync<T>(Func<CancellationToken, Task<T>> call, CancellationToken cancellationToken)
    {
        var attempts = options.Value.MaxAttempts;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await call(cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < attempts)
            {
                var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, clock, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
