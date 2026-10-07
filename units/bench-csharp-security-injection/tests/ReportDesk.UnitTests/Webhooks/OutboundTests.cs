using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using ReportDesk.Api.Http;
using ReportDesk.Api.Webhooks;

namespace ReportDesk.UnitTests.Webhooks;

public sealed class OutboundTests
{
    [Fact]
    public async Task DispatcherPostsTheEventToTheCallback()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        using var client = new HttpClient(handler);
        var dispatcher = new WebhookDispatcher(client, NullLogger<WebhookDispatcher>.Instance);

        var delivered = await dispatcher.NotifyAsync(
            new Uri("https://hooks.tenant.test/reports"), new ReportReadyEvent(Guid.NewGuid(), "Weekly", DateTimeOffset.UnixEpoch), TestContext.Current.CancellationToken);

        Assert.True(delivered);
        Assert.Equal(new Uri("https://hooks.tenant.test/reports"), Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task DispatcherRefusesPlainHttp()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var dispatcher = new WebhookDispatcher(client, NullLogger<WebhookDispatcher>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.NotifyAsync(
            new Uri("http://hooks.tenant.test/reports"), new ReportReadyEvent(Guid.NewGuid(), "Weekly", DateTimeOffset.UnixEpoch), TestContext.Current.CancellationToken));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GuardedHandlerConnectsWhenThePolicyAllowsTheAddress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = AnswerOnceAsync(listener, TestContext.Current.CancellationToken);

        using var client = new HttpClient(GuardedConnect.CreateHandler(IPAddress.IsLoopback));
        var body = await client.GetStringAsync(new Uri($"http://127.0.0.1:{port}/"), TestContext.Current.CancellationToken);

        Assert.Equal("ok", body);
        await server;
    }

    [Fact]
    public async Task GuardedHandlerRefusesAnAddressThePolicyRejects()
    {
        using var client = new HttpClient(GuardedConnect.CreateHandler(_ => false));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(new Uri("http://127.0.0.1:9/"), TestContext.Current.CancellationToken));
    }

    private static async Task AnswerOnceAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var socket = await listener.AcceptSocketAsync(cancellationToken);
        var buffer = new byte[4096];
        await socket.ReceiveAsync(buffer, cancellationToken);
        var response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        await socket.SendAsync(response, cancellationToken);
        socket.Shutdown(SocketShutdown.Both);
    }
}
