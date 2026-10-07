using Fx.Conversion.Rates.Ecb;
using Fx.Conversion.UnitTests.Fixtures;

namespace Fx.Conversion.UnitTests.Rates;

public sealed class EcbXmlParserTests
{
    [Fact]
    public void ReadsThePublicationDateAndEveryRate()
    {
        var table = EcbXmlParser.Parse(Feeds.Daily);

        Assert.Equal("EUR", table.BaseCurrency);
        Assert.Equal(new DateOnly(2026, 3, 2), table.PublishedOn);
        Assert.Equal(11, table.Rates.Count);
        Assert.Equal(1.0815m, table.Rates["USD"]);
        Assert.Equal(0.85310m, table.Rates["GBP"]);
    }

    [Fact]
    public void RejectsADocumentWithoutADatedCube()
    {
        const string xml = "<Envelope xmlns='http://www.ecb.int/vocabulary/2002-08-01/eurofxref'><Cube /></Envelope>";

        Assert.Throws<FormatException>(() => EcbXmlParser.Parse(xml));
    }

    [Fact]
    public void RejectsADayWithoutRates()
    {
        const string xml = "<Envelope xmlns='http://www.ecb.int/vocabulary/2002-08-01/eurofxref'><Cube><Cube time='2026-03-02' /></Cube></Envelope>";

        Assert.Throws<FormatException>(() => EcbXmlParser.Parse(xml));
    }

    [Fact]
    public void RejectsARateWithoutItsCurrency()
    {
        const string xml = "<Envelope xmlns='http://www.ecb.int/vocabulary/2002-08-01/eurofxref'><Cube><Cube time='2026-03-02'><Cube rate='1.1' /></Cube></Cube></Envelope>";

        Assert.Throws<FormatException>(() => EcbXmlParser.Parse(xml));
    }

    [Fact]
    public void RefusesDocumentTypeDefinitions()
    {
        const string xml = "<!DOCTYPE Envelope [<!ENTITY x 'y'>]><Envelope />";

        Assert.Throws<System.Xml.XmlException>(() => EcbXmlParser.Parse(xml));
    }
}
