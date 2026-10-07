using System.Net;
using System.Net.Sockets;

namespace ReportDesk.Api.Webhooks;

/// <summary>
/// Where a webhook callback may connect once its host name is resolved: public addresses only, so a tenant cannot
/// point a callback at the archive's own network.
/// </summary>
public static class CallbackAddressPolicy
{
    public static bool IsPublic(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !(IPAddress.IsLoopback(address)
                || address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6UniqueLocal
                || address.IsIPv6Multicast
                || address.Equals(IPAddress.IPv6Any));
        }

        var octets = address.GetAddressBytes();
        return !(octets[0] == 0
            || octets[0] == 10
            || octets[0] == 127
            || (octets[0] == 100 && octets[1] >= 64 && octets[1] <= 127)
            || (octets[0] == 169 && octets[1] == 254)
            || (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31)
            || (octets[0] == 192 && octets[1] == 168)
            || octets[0] >= 224);
    }
}
