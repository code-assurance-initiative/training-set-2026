using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Fx.Conversion.Rates.Ecb;

/// <summary>
/// Reads the European Central Bank's daily reference-rate document (<c>eurofxref-daily.xml</c>): one dated
/// <c>Cube</c> of <c>currency</c>/<c>rate</c> pairs, each the units of that currency per euro.
/// </summary>
public static class EcbXmlParser
{
    private static readonly XNamespace Ns = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";

    private static readonly XmlReaderSettings Settings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
    };

    public static RateTable Parse(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        using var reader = XmlReader.Create(new StringReader(xml), Settings);
        var document = XDocument.Load(reader);

        var (day, time) = document.Descendants(Ns + "Cube")
            .Select(cube => (Cube: cube, Time: (string?)cube.Attribute("time")))
            .FirstOrDefault(cube => cube.Time is not null);
        if (day is null || time is null)
        {
            throw new FormatException("The document has no dated Cube element.");
        }

        var publishedOn = DateOnly.ParseExact(time, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var rates = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var cube in day.Elements(Ns + "Cube"))
        {
            var currency = (string?)cube.Attribute("currency");
            var rate = (string?)cube.Attribute("rate");
            if (currency is null || rate is null)
            {
                throw new FormatException("A rate Cube lacks its currency or rate attribute.");
            }

            rates[currency] = decimal.Parse(rate, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        if (rates.Count == 0)
        {
            throw new FormatException($"The Cube of {publishedOn:yyyy-MM-dd} holds no rates.");
        }

        return new RateTable("EUR", publishedOn, rates);
    }
}
