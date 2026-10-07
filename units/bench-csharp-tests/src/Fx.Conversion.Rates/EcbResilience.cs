using System.Net;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Fx.Conversion.Rates.Ecb;

/// <summary>
/// Retry and timeout for the feed: three retries with exponential back-off (1 s, 2 s, 4 s) on network failures,
/// time-outs and 5xx/429 answers, each attempt bounded to 10 s. Time comes from the injected <see cref="TimeProvider"/>.
/// </summary>
public static class EcbResilience
{
    public const int MaxRetryAttempts = 3;
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);

    public static ResiliencePipeline Create(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return new ResiliencePipelineBuilder { TimeProvider = clock }
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = BaseDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutRejectedException>()
                    .Handle<HttpRequestException>(IsTransient),
            })
            .AddTimeout(AttemptTimeout)
            .Build();
    }

    private static bool IsTransient(HttpRequestException exception) =>
        exception.StatusCode is null
            or HttpStatusCode.TooManyRequests
            or >= HttpStatusCode.InternalServerError;
}
