using Shipping.Rates.Core.Labels;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Labels;

public sealed class AddressFormatterTests
{
    [Fact]
    public void The_company_comes_first_when_there_is_one()
    {
        Assert.Equal(["Ada GmbH", "Ada Shop", "Hauptstr. 1", "10115 Berlin", "DE"], AddressFormatter.FormatRecipient(TestData.Berlin));
        Assert.Equal(4, AddressFormatter.FormatRecipient(TestData.Paris).Count);
    }

    [Fact]
    public void A_single_line_skips_empty_parts()
    {
        Assert.Equal("Marie Client, 1 Rue de Rivoli, 75001 Paris, FR", AddressFormatter.SingleLine(TestData.Paris));
    }
}
