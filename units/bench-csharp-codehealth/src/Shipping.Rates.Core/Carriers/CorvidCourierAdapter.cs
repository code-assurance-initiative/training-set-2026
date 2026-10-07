using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Carriers;

/// <summary>Corvid Courier REST API (v1). The HttpClient is configured with the base address and API key.</summary>
public sealed class CorvidCourierAdapter : ICarrierAdapter
{
    private readonly HttpClient _http;
    private readonly ILogger<CorvidCourierAdapter> _logger;

    public CorvidCourierAdapter(HttpClient http, ILogger<CorvidCourierAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Code => "CORVID";

    public CarrierCapabilities Capabilities { get; } = new(SupportsInsurance: true, SupportsPickup: true, MaxWeightGrams: 30_000);

    public async Task<IReadOnlyList<CarrierRate>> GetRatesAsync(QuoteRequest request, CancellationToken cancellationToken)
    {
        using var message = BuildRateRequest(request);
        using var response = await _http.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Corvid rate request failed with {Status}", (int)response.StatusCode);
            throw new CarrierException(Code, ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture));
        }

        using var document = JsonDocument.Parse(body);
        return MapRates(document.RootElement, Code);
    }

    private HttpRequestMessage BuildRateRequest(QuoteRequest request)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("origin", request.Sender.PostalCode);
            writer.WriteString("originCountry", request.Sender.CountryCode);
            writer.WriteString("destination", request.Recipient.PostalCode);
            writer.WriteString("destinationCountry", request.Recipient.CountryCode);
            writer.WriteString("shipDate", request.ShipDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            // TODO: Corvid supports multi-piece shipments; we still send every parcel as its own piece and sum.
            writer.WriteStartArray("parcels");
            foreach (var parcel in request.Parcels)
            {
                writer.WriteStartObject();
                writer.WriteNumber("weightKg", parcel.WeightGrams / 1000m);
                writer.WriteNumber("lengthCm", parcel.LengthCm);
                writer.WriteNumber("widthCm", parcel.WidthCm);
                writer.WriteNumber("heightCm", parcel.HeightCm);
                writer.WriteBoolean("dangerousGoods", parcel.IsDangerousGoods);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        var content = new ByteArrayContent(buffer.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return new HttpRequestMessage(HttpMethod.Post, "v1/rates") { Content = content };
    }

    private static IReadOnlyList<CarrierRate> MapRates(JsonElement root, string carrier)
    {
        var rates = new List<CarrierRate>();
        foreach (var item in root.GetProperty("rates").EnumerateArray())
        {
            var level = ParseLevel(item.GetProperty("service").GetString());
            if (level is null)
            {
                continue;
            }

            var amount = item.GetProperty("amount").GetDecimal();
            var currency = item.GetProperty("currency").GetString() ?? "EUR";
            var transitDays = ParseTransitDays(item.GetProperty("transit").GetString());
            rates.Add(new CarrierRate(carrier, level.Value, amount, currency, transitDays));
        }

        return rates;
    }

    private static ServiceLevel? ParseLevel(string? service) => service switch
    {
        "ECO" => ServiceLevel.Economy,
        "STD" => ServiceLevel.Standard,
        "EXP" => ServiceLevel.Express,
        "ONT" => ServiceLevel.Overnight,
        _ => null,
    };

    private static int ParseTransitDays(string? transit)
    {
        if (!int.TryParse(transit, NumberStyles.None, CultureInfo.InvariantCulture, out var days))
        {
            return 3;
        }

        return days;
    }

    public async Task<CarrierLabel> CreateLabelAsync(QuoteRequest request, ServiceLevel level, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            shipper = request.Sender,
            consignee = request.Recipient,
            service = ServiceCodeMap.ToCarrierCode(Code, level, request.Sender.CountryCode != request.Recipient.CountryCode),
            pieces = request.Parcels.Select(p => new { grams = p.WeightGrams, l = p.LengthCm, w = p.WidthCm, h = p.HeightCm }),
        });

        HttpResponseMessage response;
        try
        {
            using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            response = await _http.PostAsync("v1/labels", content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw;
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            return new CarrierLabel(Code, root.GetProperty("awb").GetString() ?? string.Empty, root.GetProperty("zpl").GetString() ?? string.Empty);
        }
    }

    public Task VoidLabelAsync(string trackingNumber, CancellationToken cancellationToken) => throw new NotImplementedException();

    public async Task<TrackingStatus> TrackAsync(string trackingNumber, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(new Uri($"v1/track/{Uri.EscapeDataString(trackingNumber)}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return MapStatus(document.RootElement.GetProperty("status").GetString());
    }

    private static TrackingStatus MapStatus(string? status) => status switch
    {
        "TODO" => TrackingStatus.AwaitingCollection,
        "PICK" or "TRNS" => TrackingStatus.InTransit,
        "OFD" => TrackingStatus.OutForDelivery,
        "DLVD" => TrackingStatus.Delivered,
        "RTS" => TrackingStatus.Returned,
        _ => TrackingStatus.Unknown,
    };

    public async Task SchedulePickupAsync(Address pickupAddress, DateOnly date, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new { address = pickupAddress, date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("v1/pickups", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
