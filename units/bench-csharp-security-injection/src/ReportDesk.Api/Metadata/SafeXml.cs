using System.Xml;

namespace ReportDesk.Api.Metadata;

/// <summary>Reader settings for XML the service did not write: no DTD, no resolver, bounded entity expansion.</summary>
public static class SafeXml
{
    public static XmlReaderSettings ReaderSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 0,
        MaxCharactersInDocument = 10_000_000,
    };

    public static XmlDocument LoadDocument(string xml)
    {
        var document = new XmlDocument { XmlResolver = null };
        using var reader = XmlReader.Create(new StringReader(xml), ReaderSettings());
        document.Load(reader);
        return document;
    }
}
