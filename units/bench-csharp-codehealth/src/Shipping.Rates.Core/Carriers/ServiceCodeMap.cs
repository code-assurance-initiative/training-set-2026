using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Carriers;

/// <summary>Each carrier's product code for a service level, domestic or cross-border.</summary>
public static class ServiceCodeMap
{
    public static string ToCarrierCode(string carrier, ServiceLevel level, bool international)
    {
        switch ((carrier, level, international))
        {
            case ("ALDER", ServiceLevel.Economy, false): return "ALD-ECO";
            case ("ALDER", ServiceLevel.Economy, true): return "ALD-ECO-INT";
            case ("ALDER", ServiceLevel.Standard, false): return "ALD-STD";
            case ("ALDER", ServiceLevel.Standard, true): return "ALD-STD-INT";
            case ("ALDER", ServiceLevel.Express, false): return "ALD-EXP";
            case ("ALDER", ServiceLevel.Express, true): return "ALD-EXP-INT";
            case ("ALDER", ServiceLevel.Overnight, false): return "ALD-ONT";
            case ("ALDER", ServiceLevel.Overnight, true): return "ALD-ONT-INT";
            case ("CORVID", ServiceLevel.Economy, false): return "ECO";
            case ("CORVID", ServiceLevel.Economy, true): return "ECO-X";
            case ("CORVID", ServiceLevel.Standard, false): return "STD";
            case ("CORVID", ServiceLevel.Standard, true): return "STD-X";
            case ("CORVID", ServiceLevel.Express, false): return "EXP";
            case ("CORVID", ServiceLevel.Express, true): return "EXP-X";
            case ("CORVID", ServiceLevel.Overnight, false): return "ONT";
            case ("CORVID", ServiceLevel.Overnight, true): return "ONT-X";
            default: throw new ArgumentOutOfRangeException(nameof(level), level, $"{carrier} has no product for {level}.");
        }
    }
}
