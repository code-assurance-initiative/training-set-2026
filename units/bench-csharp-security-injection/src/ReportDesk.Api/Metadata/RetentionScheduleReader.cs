using System.Globalization;
using System.Xml;

namespace ReportDesk.Api.Metadata;

/// <summary>
/// Reads a retention schedule uploaded by records management:
/// <c>&lt;retention&gt;&lt;rule class="HR" years="7"/&gt;…&lt;/retention&gt;</c>.
/// </summary>
public static class RetentionScheduleReader
{
    public static IReadOnlyDictionary<string, int> Read(Stream xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
        };

        var rules = new Dictionary<string, int>(StringComparer.Ordinal);
        using var reader = XmlReader.Create(xml, settings);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "rule")
            {
                var classification = reader.GetAttribute("class") ?? throw new XmlException("A rule needs a class.");
                var years = int.Parse(reader.GetAttribute("years") ?? "0", NumberStyles.None, CultureInfo.InvariantCulture);
                rules[classification] = years;
            }
        }

        return rules;
    }
}
