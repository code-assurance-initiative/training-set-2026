using System.Net;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Domain;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Carriers;

public sealed class CarrierRegistryTests
{
    private static CarrierRegistry Registry()
    {
        var alder = new AlderParcelAdapter(new StubHandler(HttpStatusCode.OK, "{}").Client(), Loggers.For<AlderParcelAdapter>());
        return new CarrierRegistry([alder], Loggers.For<CarrierRegistry>());
    }

    [Fact]
    public void Codes_are_resolved_case_insensitively()
    {
        Assert.Equal("ALDER", Registry().Resolve("alder").Code);
    }

    [Fact]
    public void Unknown_or_unregistered_codes_are_refused()
    {
        Assert.Throws<UnknownCarrierException>(() => Registry().Resolve("dhl"));
        Assert.Throws<UnknownCarrierException>(() => Registry().Resolve("corvid"));
    }

    [Fact]
    public void Added_adapters_become_resolvable()
    {
        var registry = Registry();
        registry.Add(new CorvidCourierAdapter(new StubHandler(HttpStatusCode.OK, "{}").Client(), Loggers.For<CorvidCourierAdapter>()));

        Assert.Equal(2, registry.All.Count);
        Assert.Equal("CORVID", registry.Resolve("Corvid").Code);
    }

    [Fact]
    public void Display_names_are_known_for_corvid()
    {
        Assert.Equal("Corvid Courier", CarrierRegistry.DisplayName("corvid"));
        Assert.Equal("DHL", CarrierRegistry.DisplayName("DHL"));
    }

    [Theory]
    [InlineData("ALDER", ServiceLevel.Economy, false, "ALD-ECO")]
    [InlineData("ALDER", ServiceLevel.Economy, true, "ALD-ECO-INT")]
    [InlineData("ALDER", ServiceLevel.Standard, false, "ALD-STD")]
    [InlineData("ALDER", ServiceLevel.Standard, true, "ALD-STD-INT")]
    [InlineData("ALDER", ServiceLevel.Express, false, "ALD-EXP")]
    [InlineData("ALDER", ServiceLevel.Express, true, "ALD-EXP-INT")]
    [InlineData("ALDER", ServiceLevel.Overnight, false, "ALD-ONT")]
    [InlineData("ALDER", ServiceLevel.Overnight, true, "ALD-ONT-INT")]
    [InlineData("CORVID", ServiceLevel.Economy, false, "ECO")]
    [InlineData("CORVID", ServiceLevel.Economy, true, "ECO-X")]
    [InlineData("CORVID", ServiceLevel.Standard, false, "STD")]
    [InlineData("CORVID", ServiceLevel.Standard, true, "STD-X")]
    [InlineData("CORVID", ServiceLevel.Express, false, "EXP")]
    [InlineData("CORVID", ServiceLevel.Express, true, "EXP-X")]
    [InlineData("CORVID", ServiceLevel.Overnight, false, "ONT")]
    [InlineData("CORVID", ServiceLevel.Overnight, true, "ONT-X")]
    public void Service_codes_follow_the_carrier_catalogues(string carrier, ServiceLevel level, bool international, string expected)
    {
        Assert.Equal(expected, ServiceCodeMap.ToCarrierCode(carrier, level, international));
    }

    [Fact]
    public void A_carrier_without_a_catalogue_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCodeMap.ToCarrierCode("DHL", ServiceLevel.Economy, false));
    }
}
