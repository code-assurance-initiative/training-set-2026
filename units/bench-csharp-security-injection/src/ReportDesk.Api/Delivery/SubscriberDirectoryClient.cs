using System.Net.Http.Json;

namespace ReportDesk.Api.Delivery;

public sealed record CrmContact(string Id, string DisplayName, string? Company);

/// <summary>The CRM that owns subscriber contact records and their notification channels.</summary>
public sealed class SubscriberDirectoryClient(HttpClient client)
{
    public Task<CrmContact?> FindContactAsync(string emailAddress, CancellationToken cancellationToken) =>
        client.GetFromJsonAsync<CrmContact>($"contacts?email={Uri.EscapeDataString(emailAddress)}", cancellationToken);

    public async Task SetEmailChannelAsync(Guid subscriptionId, bool enabled, CancellationToken cancellationToken)
    {
        var flag = enabled ? "true" : "false";
        using var response = await client
            .PutAsync($"subscriptions/{subscriptionId:N}/channels?email={flag}", content: null, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
