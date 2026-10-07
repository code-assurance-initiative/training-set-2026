using System.Net;
using System.Net.Sockets;

namespace ReportDesk.Api.Feeds;

/// <summary>
/// Where a partner feed connection may go once its host name is resolved. Partner endpoints are IPv4-only, so an
/// IPv6 answer is accepted only when it is an IPv4 address in mapped form; every other IPv6 address is refused.
/// </summary>
public static class FeedAddressPolicy
{
    public static bool IsPublicIPv4Destination(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (!address.IsIPv4MappedToIPv6)
            {
                return false;
            }

            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
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
