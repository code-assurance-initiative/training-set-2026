using Shipping.Rates.Core.Tracking;

namespace Shipping.Rates.UnitTests.Tracking;

public sealed class TrackingNumberTests
{
    [Theory]
    [InlineData("A1234567890123", "ALDER")]
    [InlineData("L9988776655", "ALDER")]
    [InlineData("JJD0001234567", "CORVID")]
    [InlineData("1Z999AA10123456784", null)]
    public void The_issuing_carrier_is_recognised(string value, string? expected)
    {
        Assert.Equal(expected, TrackingNumber.CarrierOf(value));
    }

    [Theory]
    [InlineData("A1234567890123", true)]
    [InlineData("a123", false)]
    [InlineData("", false)]
    public void Well_formed_numbers_are_upper_case_alphanumerics(string value, bool expected)
    {
        Assert.Equal(expected, TrackingNumber.IsWellFormed(value));
    }

    [Theory]
    [InlineData("R123", true)]
    [InlineData("A123RT", true)]
    [InlineData("A123", false)]
    [InlineData("", false)]
    public void Return_labels_are_recognised(string value, bool expected)
    {
        Assert.Equal(expected, TrackingNumber.IsReturnLabel(value));
    }
}
