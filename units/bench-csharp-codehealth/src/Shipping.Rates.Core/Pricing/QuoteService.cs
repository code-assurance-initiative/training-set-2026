using Microsoft.Extensions.Logging;
using Shipping.Rates.Core.Accounts;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Pricing;

/// <summary>
/// Asks every eligible carrier which services it can run for a shipment, and prices each one from our published
/// tariff. Transit times come from the carrier; prices come from the tariff.
/// </summary>
public sealed class QuoteService
{
    private readonly CarrierRegistry _registry;
    private readonly MultiParcelQuoter _quoter;
    private readonly CarrierAccountManager _accounts;
    private readonly TimeProvider _clock;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(CarrierRegistry registry, MultiParcelQuoter quoter, CarrierAccountManager accounts, TimeProvider clock, ILogger<QuoteService> logger)
    {
        _registry = registry;
        _quoter = quoter;
        _accounts = accounts;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RateQuote>> QuoteAsync(QuoteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var quotes = new List<RateQuote>();
        foreach (var adapter in _registry.All)
        {
            if (!IsEligible(adapter.Capabilities, request))
            {
                continue;
            }

            if (!_accounts.TryConsumeRequest(adapter.Code, _clock.GetUtcNow()))
            {
                _logger.LogWarning("Rate limit reached for {Carrier}; skipping it for this quote", adapter.Code);
                continue;
            }

            var rates = await adapter.GetRatesAsync(request, cancellationToken);
            foreach (var rate in rates)
            {
                var price = _quoter.QuoteShipment(adapter.Code, rate.Level, request.Parcels, request.Recipient);
                quotes.Add(new RateQuote(adapter.Code, rate.Level, price, rate.TransitDays));
            }

            _accounts.RecordAudit($"quoted {request.Parcels.Count} parcel(s) with {adapter.Code}", _clock.GetUtcNow());
        }

        _logger.LogDebug("Quotes: " + string.Join(", ", quotes.Select(q => $"{q.Carrier}/{q.Level}={q.Total}")));
        if (request.MaxPrice is decimal ceiling)
        {
            RemoveAboveCeiling(quotes, ceiling);
        }

        return [.. quotes.OrderBy(q => q.Total.Amount).ThenBy(q => q.TransitDays)];
    }

    private static void RemoveAboveCeiling(List<RateQuote> quotes, decimal ceiling)
    {
        foreach (var quote in quotes)
        {
            if (quote.Total.Amount > ceiling)
            {
                quotes.Remove(quote);
            }
        }
    }

    private static bool IsEligible(CarrierCapabilities capabilities, QuoteRequest request)
    {
        if (request.Parcels.Any(p => p.WeightGrams > capabilities.MaxWeightGrams))
        {
            return false;
        }

        if (!(capabilities.SupportsInsurance || request.InsuredValue != 0))
        {
            return false;
        }

        return true;
    }
}
