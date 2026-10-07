using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Shipping.Rates.Core.Carriers.Alder;
using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Carriers;

/// <summary>Alder Parcel REST API (v2). The HttpClient is configured with the base address and API key.</summary>
public sealed class AlderParcelAdapter : ICarrierAdapter
{
    private readonly HttpClient _http;
    private readonly ILogger<AlderParcelAdapter> _logger;

    public AlderParcelAdapter(HttpClient http, ILogger<AlderParcelAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Code => "ALDER";

    public CarrierCapabilities Capabilities { get; } = new(SupportsInsurance: true, SupportsPickup: false, MaxWeightGrams: 31_500);

    public async Task<IReadOnlyList<CarrierRate>> GetRatesAsync(QuoteRequest request, CancellationToken cancellationToken)
    {
        using var message = BuildRateRequest(request);
        using var response = await _http.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorCode = ExtractErrorCode(body);
            _logger.LogWarning("Alder rate request failed with {Status} ({ErrorCode})", (int)response.StatusCode, errorCode);
            throw new CarrierException(Code, errorCode);
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
            writer.WriteStartArray("parcels");
            foreach (var parcel in request.Parcels)
            {
                writer.WriteStartObject();
                writer.WriteNumber("weightKg", Math.Ceiling(parcel.WeightGrams / 100m) / 10m);
                writer.WriteNumber("lengthCm", parcel.LengthCm);
                writer.WriteNumber("widthCm", parcel.WidthCm);
                writer.WriteNumber("heightCm", parcel.HeightCm);
                writer.WriteBoolean("dangerousGoods", parcel.IsDangerousGoods);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteNumber("insuredValue", request.InsuredValue);
            writer.WriteEndObject();
        }

        var content = new ByteArrayContent(buffer.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return new HttpRequestMessage(HttpMethod.Post, "v2/rates") { Content = content };
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
            var transitDays = item.GetProperty("transitDays").GetInt32();
            rates.Add(new CarrierRate(carrier, level.Value, amount, currency, transitDays));
        }

        return rates;
    }

    private static ServiceLevel? ParseLevel(string? service) => service switch
    {
        "economy" => ServiceLevel.Economy,
        "standard" => ServiceLevel.Standard,
        "express" => ServiceLevel.Express,
        "overnight" => ServiceLevel.Overnight,
        _ => null,
    };

    private static string ExtractErrorCode(string body)
    {
        var match = Regex.Match(body, "\"errorCode\"\\s*:\\s*\"([A-Z_]+)\"");
        return match.Success ? match.Groups[1].Value : "UNKNOWN";
    }

    public async Task<CarrierLabel> CreateLabelAsync(QuoteRequest request, ServiceLevel level, CancellationToken cancellationToken)
    {
        var international = request.Sender.CountryCode != request.Recipient.CountryCode;
        var shipment = new AlderShipment(
            ToWire(request.Sender),
            ToWire(request.Recipient),
            ServiceCodeMap.ToCarrierCode(Code, level, international),
            [.. request.Parcels.Select(p => new AlderPiece(p.WeightGrams / 1000m, p.LengthCm, p.WidthCm, p.HeightCm))],
            request.InsuredValue);

        using var response = await _http.PostAsJsonAsync("v2/shipments", shipment, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CarrierException(Code, ExtractErrorCode(await response.Content.ReadAsStringAsync(cancellationToken)));
        }

        var label = await response.Content.ReadFromJsonAsync<AlderLabelResponse>(cancellationToken)
            ?? throw new CarrierException(Code, "EMPTY_RESPONSE");
        return new CarrierLabel(Code, label.TrackingNumber, label.LabelZpl);
    }

    private static AlderAddress ToWire(Address address) =>
        new(address.Name, address.Company, address.Street, address.PostalCode, address.City, address.CountryCode);

    public async Task VoidLabelAsync(string trackingNumber, CancellationToken cancellationToken)
    {
        using var response = await _http.DeleteAsync(new Uri($"v2/shipments/{Uri.EscapeDataString(trackingNumber)}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TrackingStatus> TrackAsync(string trackingNumber, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(new Uri($"v2/tracking/{Uri.EscapeDataString(trackingNumber)}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("state").GetString() switch
        {
            "created" => TrackingStatus.AwaitingCollection,
            "in_transit" => TrackingStatus.InTransit,
            "out_for_delivery" => TrackingStatus.OutForDelivery,
            "delivered" => TrackingStatus.Delivered,
            "returned" => TrackingStatus.Returned,
            _ => TrackingStatus.Unknown,
        };
    }

    public Task SchedulePickupAsync(Address pickupAddress, DateOnly date, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Alder Parcel does not collect: parcels are handed in at an Alder service point.");
}
