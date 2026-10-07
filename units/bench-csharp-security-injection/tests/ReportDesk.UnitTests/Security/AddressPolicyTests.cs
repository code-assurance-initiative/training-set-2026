using System.Net;
using ReportDesk.Api.Feeds;
using ReportDesk.Api.Webhooks;

namespace ReportDesk.UnitTests.Security;

public sealed class AddressPolicyTests
{
    [Theory]
    [InlineData("93.184.216.34", true)]
    [InlineData("10.1.2.3", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("172.20.0.5", false)]
    [InlineData("192.168.1.10", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("::ffff:10.1.2.3", false)]
    [InlineData("::ffff:93.184.216.34", true)]
    [InlineData("2001:4860:4860::8888", false)]
    [InlineData("64:ff9b::a01:203", false)]
    public void FeedConnectionsGoToPublicIPv4Only(string address, bool allowed)
    {
        Assert.Equal(allowed, FeedAddressPolicy.IsPublicIPv4Destination(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("93.184.216.34", true)]
    [InlineData("2001:4860:4860::8888", true)]
    [InlineData("10.1.2.3", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("::ffff:192.168.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("fd00::1", false)]
    public void WebhookCallbacksGoToPublicAddresses(string address, bool allowed)
    {
        Assert.Equal(allowed, CallbackAddressPolicy.IsPublic(IPAddress.Parse(address)));
    }
}
