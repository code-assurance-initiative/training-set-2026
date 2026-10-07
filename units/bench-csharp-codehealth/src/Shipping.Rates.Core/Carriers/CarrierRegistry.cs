using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Shipping.Rates.Core.Carriers;

/// <summary>The carrier adapters this host was configured with, looked up by carrier code.</summary>
public sealed class CarrierRegistry
{
    private readonly List<ICarrierAdapter> _adapters;
    private readonly ILogger<CarrierRegistry> _logger;

    public CarrierRegistry(IEnumerable<ICarrierAdapter> adapters, ILogger<CarrierRegistry> logger)
    {
        _adapters = [.. adapters];
        _logger = logger;
    }

    public IReadOnlyList<ICarrierAdapter> All => _adapters;

    public ICarrierAdapter Resolve(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        switch (code.ToUpperInvariant())
        {
            case "ALDER":
                return Find("ALDER");
            case "CORVID":
                return Find("CORVID");
            default:
                throw new UnknownCarrierException(code);
        }
    }

    public static string DisplayName(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        switch (code.ToUpperInvariant())
        {
            case "CORVID":
                return "Corvid Courier";
            case "alder":
                return "Alder Parcel";
            default:
                return code;
        }
    }

    public void Add(ICarrierAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapters.Add(adapter);
        _logger.LogInformation($"Registered carrier {adapter.Code}; {_adapters.Count} adapters are now available");
    }

    private ICarrierAdapter Find(string code) =>
        _adapters.Find(a => a.Code == code) ?? throw new UnknownCarrierException(code);

    /// <summary>
    /// Creates a partner-supplied adapter by type name. Adapters loaded this way are not yet covered by the
    /// carrier certification suite.
    /// </summary>
    [Experimental("SHIPRATES001")]
    public static ICarrierAdapter CreateCustom(string adapterTypeName, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == adapterTypeName && typeof(ICarrierAdapter).IsAssignableFrom(t))
            ?? throw new UnknownCarrierException(adapterTypeName);
        return services.GetService(type) as ICarrierAdapter ?? throw new UnknownCarrierException(adapterTypeName);
    }
}
