using System.Net;
using System.Net.Sockets;

namespace ReportDesk.Api.Http;

/// <summary>
/// Builds a handler that resolves the destination itself and connects only to an address the given policy accepts.
/// The check runs inside the connect callback, so the address that is checked is the address that is connected to.
/// </summary>
public static class GuardedConnect
{
    public static SocketsHttpHandler CreateHandler(Func<IPAddress, bool> isAllowed)
    {
        ArgumentNullException.ThrowIfNull(isAllowed);
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
                var allowed = Array.Find(addresses, address => isAllowed(address))
                    ?? throw new HttpRequestException($"No permitted address for host '{context.DnsEndPoint.Host}'.");

                var socket = new Socket(allowed.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(allowed, context.DnsEndPoint.Port), cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
    }
}
