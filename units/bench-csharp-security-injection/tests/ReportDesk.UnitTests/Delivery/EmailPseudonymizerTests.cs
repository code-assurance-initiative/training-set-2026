using ReportDesk.Api.Delivery;

namespace ReportDesk.UnitTests.Delivery;

public sealed class EmailPseudonymizerTests
{
    private readonly EmailPseudonymizer _pseudonymizer = new(TestOptions.Delivery(new string('k', 32)));

    [Fact]
    public void GivesTheSameAddressTheSamePseudonymRegardlessOfCase()
    {
        Assert.Equal(_pseudonymizer.Hash("Ann.Berg@archive.test"), _pseudonymizer.Hash(" ann.berg@ARCHIVE.test "));
    }

    [Fact]
    public void DoesNotRevealTheAddress()
    {
        var pseudonym = _pseudonymizer.Hash("ann.berg@archive.test");
        Assert.Equal(16, pseudonym.Length);
        Assert.DoesNotContain("berg", pseudonym, StringComparison.OrdinalIgnoreCase);
    }
}
