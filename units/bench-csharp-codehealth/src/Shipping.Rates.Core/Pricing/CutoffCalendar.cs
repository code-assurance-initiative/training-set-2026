using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shipping.Rates.Core.Pricing;

/// <summary>Decides the ship date of a booking from the daily cut-off; weekends are not working days.</summary>
public sealed class CutoffCalendar
{
    /// <summary>The cut-off used when none (or an unreadable one) is configured.</summary>
    public static readonly TimeOnly DefaultCutoff = new(16, 0);

    private readonly ILogger<CutoffCalendar> _logger;

    public CutoffCalendar(IOptions<ShippingOptions> options, ILogger<CutoffCalendar> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        Cutoff = ParseCutoff(options.Value.DailyCutoff);
    }

    public TimeOnly Cutoff { get; }

    public DateOnly ShipDateFor(DateTime bookedAtLocal)
    {
        var date = DateOnly.FromDateTime(bookedAtLocal);
        if (TimeOnly.FromDateTime(bookedAtLocal) >= Cutoff)
        {
            date = date.AddDays(1);
        }

        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private TimeOnly ParseCutoff(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return DefaultCutoff;
        }

        if (!TimeOnly.TryParseExact(configured, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var cutoff))
        {
            _logger.LogWarning("Ignoring unreadable Shipping:DailyCutoff {Configured}; using {Default}", configured, DefaultCutoff);
            return DefaultCutoff;
        }

        return cutoff;
    }
}
