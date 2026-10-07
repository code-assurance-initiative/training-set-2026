using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Pricing;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Pricing;

public sealed class MultiParcelQuoterTests
{
    private readonly MultiParcelQuoter _quoter = new(TestData.Calculator(), TestData.Policy());

    [Fact]
    public void A_shipment_costs_the_sum_of_its_parcels()
    {
        var total = _quoter.QuoteShipment("ALDER", ServiceLevel.Standard, [TestData.Small, TestData.Small], TestData.Paris);

        Assert.Equal(new Money(14.00m, "EUR"), total);
    }

    [Fact]
    public void Dangerous_goods_add_the_handling_fee()
    {
        var dangerous = TestData.Small with { IsDangerousGoods = true };

        var total = _quoter.QuoteShipment("ALDER", ServiceLevel.Standard, [dangerous], TestData.Paris);

        Assert.Equal(new Money(7.00m + 48.00m, "EUR"), total);
    }

    [Fact]
    public void Two_oversize_parcels_are_accepted()
    {
        var total = _quoter.QuoteShipment("ALDER", ServiceLevel.Express, [TestData.Oversize, TestData.Oversize], TestData.Paris);

        Assert.True(total.Amount > 0);
    }

    [Fact]
    public void An_absent_parcel_list_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => _quoter.QuoteShipment("ALDER", ServiceLevel.Standard, null!, TestData.Paris));
    }
}
