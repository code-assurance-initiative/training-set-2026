using System.Net.Http.Headers;
using DocumentExport.Api.Exports;
using Microsoft.Extensions.Options;

namespace DocumentExport.Api.Notifications;

/// <summary>Typed HTTP client of the notification relay.</summary>
public sealed class DeliveryNotifier : IDeliveryNotifier
{
    private readonly HttpClient _httpClient;
    private readonly NotificationOptions _options;
    private readonly ILogger<DeliveryNotifier> _logger;

    public DeliveryNotifier(HttpClient httpClient, IOptions<NotificationOptions> options, ILogger<DeliveryNotifier> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "3GhgwdAAnU1z40mkX_IlS8BE1TeHoEx-TIVS5RQNtzk");
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExportReadyAsync(ExportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        var message = new
        {
            channel = _options.Channel,
            text = $"Inventory export {job.Id:D} for {job.WarehouseCode} is ready.",
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.RelayUrl, message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("The notification relay answered {StatusCode} for export {ExportId}", (int)response.StatusCode, job.Id);
        }
    }
}
