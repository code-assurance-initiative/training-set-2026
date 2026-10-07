using Microsoft.Extensions.DependencyInjection;

namespace ParcelTracking.Client;

/// <summary>Registration of <see cref="ParcelTrackingClient"/> with IHttpClientFactory.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ParcelTrackingClient"/> as a typed client. Returns the <see cref="IHttpClientBuilder"/> so
    /// the application adds its own authentication handler and resilience policy (for example
    /// <c>AddStandardResilienceHandler()</c>): only the application knows which of its calls may be retried.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseAddress">The API root, e.g. https://parcels.example.net/.</param>
    /// <returns>The builder for further configuration.</returns>
    public static IHttpClientBuilder AddParcelTrackingClient(this IServiceCollection services, Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(baseAddress);
        return services.AddHttpClient<ParcelTrackingClient>(client => client.BaseAddress = baseAddress);
    }
}
