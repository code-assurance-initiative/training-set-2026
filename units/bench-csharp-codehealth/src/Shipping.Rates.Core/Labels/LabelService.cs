using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Pricing;

namespace Shipping.Rates.Core.Labels;

public sealed record LabelRequest(QuoteRequest Quote, string Carrier, string? Email, string? Reference, string? AccountId);

public sealed record LabelResult(string LabelId, string Carrier, string TrackingNumber, Money Price, IReadOnlyList<string> Warnings);

public sealed class LabelValidationException(IReadOnlyList<string> errors) : Exception(string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public sealed class LabelService
{
    private static readonly JsonSerializerOptions IndexJson = new() { WriteIndented = true };

    private readonly RateCalculator _calculator;
    private readonly CarrierRegistry _registry;
    private readonly LabelArchive _archive;
    private readonly CutoffCalendar _calendar;
    private readonly LabelOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<LabelService> _logger;
    private readonly Lock _indexLock = new();
    private readonly Dictionary<string, (int Count, decimal Revenue)> _stats = new(StringComparer.Ordinal);
    private int _sequence;

    public LabelService(
        RateCalculator calculator,
        CarrierRegistry registry,
        LabelArchive archive,
        CutoffCalendar calendar,
        IOptions<LabelOptions> options,
        TimeProvider clock,
        ILogger<LabelService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _calculator = calculator;
        _registry = registry;
        _archive = archive;
        _calendar = calendar;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<LabelResult> CreateLabelAsync(JsonElement body, CancellationToken cancellationToken)
    {
        var request = ParseRequest(body);
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            throw new LabelValidationException(errors);
        }

        var carrier = SelectCarrier(request.Carrier);
        var price = ApplyAccountDiscount(PriceShipment(request), request.AccountId);
        var carrierLabel = await carrier.CreateLabelAsync(request.Quote, request.Quote.Level, cancellationToken);
        var labelId = NextLabelNumber();
        var content = BuildContent(labelId, carrier.Code, carrierLabel.TrackingNumber, request);
        var zpl = ZplLabelRenderer.Render(content);
        WriteLabelFile(labelId, zpl);

        var record = new LabelRecord
        {
            LabelId = labelId,
            Carrier = carrier.Code,
            TrackingNumber = carrierLabel.TrackingNumber,
            ServiceCode = content.ServiceCode,
            RecipientEmail = request.Email!,
            Price = price.Amount,
            Currency = price.Currency,
            CreatedAt = _clock.GetUtcNow(),
        };
        AppendToIndex(record);
        RecordStats(carrier.Code, price.Amount);

        var warnings = new List<string>();
        if (record.RecipientEmail != null)
        {
            await SendLabelEmailAsync(record, zpl, cancellationToken);
        }
        else
        {
            warnings.Add("No e-mail address given; the label was not sent.");
        }

        return new LabelResult(labelId, carrier.Code, carrierLabel.TrackingNumber, price, warnings);
    }

    public LabelRequest ParseRequest(JsonElement body)
    {
        var sender = ParseAddress(body, "sender");
        var recipient = ParseAddress(body, "recipient");
        var parcels = ParseParcels(body);
        var level = ParseLevel(body.TryGetProperty("service", out var s) ? s.GetString() : null);
        var insured = body.TryGetProperty("insuredValue", out var iv) ? iv.GetDecimal() : 0m;
        var shipDate = _calendar.ShipDateFor(_clock.GetLocalNow().DateTime);
        var quote = new QuoteRequest(sender, recipient, parcels, level, shipDate, insured);
        var carrier = body.TryGetProperty("carrier", out var c) ? c.GetString() ?? string.Empty : string.Empty;
        return new LabelRequest(
            quote,
            carrier,
            body.TryGetProperty("email", out var e) ? e.GetString() : null,
            body.TryGetProperty("reference", out var r) ? r.GetString() : null,
            body.TryGetProperty("account", out var a) ? a.GetString() : null);
    }

    private static Address ParseAddress(JsonElement body, string name)
    {
        if (!body.TryGetProperty(name, out var el))
        {
            return null!;
        }

        string get_str(string p) => el.TryGetProperty(p, out var v) ? v.GetString() ?? string.Empty : string.Empty;
        return new Address(get_str("name"), el.TryGetProperty("company", out var co) ? co.GetString() : null, get_str("street"), get_str("postalCode"), get_str("city"), get_str("country").ToUpperInvariant());
    }

    private static List<Parcel> ParseParcels(JsonElement body)
    {
        var lstParcels = new List<Parcel>();
        if (!body.TryGetProperty("parcels", out var arr))
        {
            return lstParcels;
        }

        foreach (var p in arr.EnumerateArray())
        {
            lstParcels.Add(new Parcel(
                p.GetProperty("weightGrams").GetInt32(),
                p.GetProperty("lengthCm").GetInt32(),
                p.GetProperty("widthCm").GetInt32(),
                p.GetProperty("heightCm").GetInt32(),
                p.TryGetProperty("dangerousGoods", out var dg) && dg.GetBoolean()));
        }

        return lstParcels;
    }

    private static ServiceLevel ParseLevel(string? value) =>
        Enum.TryParse<ServiceLevel>(value, ignoreCase: true, out var level) ? level : ServiceLevel.Standard;

    public List<string> Validate(LabelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();
        ValidateAddress(request.Quote.Sender, "sender", errors);
        ValidateAddress(request.Quote.Recipient, "recipient", errors);
        if (request.Quote.Parcels.Count == 0)
        {
            errors.Add("At least one parcel is required.");
        }

        for (var i = 0; i < request.Quote.Parcels.Count; i++)
        {
            ValidateParcel(request.Quote.Parcels[i], i, errors);
        }

        if (request.Email != null && !IsValidEmail(request.Email))
        {
            errors.Add("email is not a valid address.");
        }

        if (string.IsNullOrWhiteSpace(request.Carrier))
        {
            errors.Add("carrier is required.");
        }

        return errors;
    }

    private static void ValidateAddress(Address address, string prefix, List<string> errors)
    {
        if (address == null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(address.Name)) errors.Add($"{prefix}.name is required.");
        if (string.IsNullOrWhiteSpace(address.Street)) errors.Add($"{prefix}.street is required.");
        if (address.CountryCode.Length != 2) errors.Add($"{prefix}.country must be an ISO 3166 alpha-2 code.");
        if (!IsValidPostalCode(address.CountryCode, address.PostalCode)) errors.Add($"{prefix}.postalCode is not valid for {address.CountryCode}.");
    }

    private static bool IsValidPostalCode(string country, string postalCode)
    {
        var pattern = country switch
        {
            "DE" or "FR" or "IT" or "ES" or "FI" => "^[0-9]{5}$",
            "AT" or "BE" or "DK" or "CH" or "LU" or "NO" => "^[0-9]{4}$",
            "NL" => "^[0-9]{4} ?[A-Z]{2}$",
            "GB" => "^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$",
            _ => "^[A-Z0-9 -]{2,10}$",
        };
        return Regex.IsMatch(postalCode, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }

    private static bool IsValidEmail(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < email.Length - 3 && email.IndexOf('.', at) > at + 1;
    }

    private static void ValidateParcel(Parcel parcel, int index, List<string> errors)
    {
        // check the weight
        if (parcel.WeightGrams <= 0) errors.Add($"parcels[{index}].weightGrams must be positive.");
        if (parcel.LengthCm <= 0 || parcel.WidthCm <= 0 || parcel.HeightCm <= 0) errors.Add($"parcels[{index}] dimensions must be positive.");
    }

    private Money PriceShipment(LabelRequest request)
    {
        var mTotal = Money.Zero(_options.Currency);
        foreach (var parcel in request.Quote.Parcels)
        {
            mTotal += _calculator.Quote(request.Carrier.ToUpperInvariant(), parcel.WeightGrams, request.Quote.Recipient.CountryCode);
        }

        return mTotal + _calculator.InsuranceFor(request.Carrier.ToUpperInvariant(), request.Quote.InsuredValue);
    }

    private Money ApplyAccountDiscount(Money price, string? accountId)
    {
        if (accountId == null || !_options.AccountDiscounts.TryGetValue(accountId, out var pct))
        {
            return price;
        }

        // apply the discount
        return (price with { Amount = price.Amount * (1 - (pct / 100m)) }).Rounded();
    }

    private ICarrierAdapter SelectCarrier(string code)
    {
        var adapter = _registry.All.FirstOrDefault(a => a.Code == code.ToUpperInvariant())!;
        _logger.LogInformation("Creating label with {Carrier}", adapter.Code);
        return adapter;
    }

    private static string ResolveServiceCode(string carrier, QuoteRequest quote) =>
        ServiceCodeMap.ToCarrierCode(carrier, quote.Level, quote.Sender.CountryCode != quote.Recipient.CountryCode);

    private string NextLabelNumber()
    {
        var n = Interlocked.Increment(ref _sequence);
        var stamp = _clock.GetUtcNow().ToString("yyMMdd", CultureInfo.InvariantCulture);
        // HACK: Corvid's validator rejects label ids shorter than 14 characters, so every id is padded for everyone.
        var tmp = string.Create(CultureInfo.InvariantCulture, $"{_options.LabelPrefix}{stamp}{n:D6}");
        return tmp.PadRight(14, '0');
        // var legacyNumber = $"{_options.LabelPrefix}{DateTime.UtcNow:yyMMddHHmmss}";
        // if (legacyNumber.Length > 16)
        // {
        //     legacyNumber = legacyNumber[..16];
        // }
        // return legacyNumber;
    }

    private LabelContent BuildContent(string labelId, string carrier, string trackingNumber, LabelRequest request)
    {
        var quote = request.Quote;
        var customs = quote.Sender.CountryCode != quote.Recipient.CountryCode
            && Pricing.Generated.CountryZones.RequiresCustomsDeclaration(quote.Recipient.CountryCode);
        return new LabelContent(
            labelId,
            carrier,
            ResolveServiceCode(carrier, quote),
            trackingNumber,
            quote.Sender,
            AddressFormatter.FormatRecipient(quote.Recipient),
            quote.Parcels[0],
            1,
            quote.Parcels.Count,
            quote.ShipDate,
            request.Reference,
            customs,
            quote.InsuredValue,
            _options.Currency);
    }

    private void WriteLabelFile(string labelId, string zpl)
    {
        try
        {
            _archive.Save(labelId, zpl);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Label {LabelId} could not be written", labelId);
            throw ex;
        }
    }

    private string IndexPath => Path.Combine(_archive.Root, "index.json");

    private List<LabelRecord> LoadIndex()
    {
        if (!File.Exists(IndexPath))
        {
            return [];
        }

        var json = File.ReadAllText(IndexPath);
        return JsonSerializer.Deserialize<List<LabelRecord>>(json) ?? [];
    }

    private void SaveIndex(List<LabelRecord> records)
    {
        Directory.CreateDirectory(_archive.Root);
        File.WriteAllText(IndexPath, JsonSerializer.Serialize(records, IndexJson));
    }

    private void AppendToIndex(LabelRecord record)
    {
        lock (_indexLock)
        {
            var records = LoadIndex();
            records.Add(record);
            SaveIndex(records);
        }
    }

    public LabelRecord? FindLabel(string labelId)
    {
        lock (_indexLock)
        {
            return LoadIndex().Find(r => r.LabelId == labelId);
        }
    }

    public IReadOnlyList<LabelRecord> ListLabels(int take)
    {
        lock (_indexLock)
        {
            return [.. LoadIndex().OrderByDescending(r => r.CreatedAt).Take(take)];
        }
    }

    /// <summary>Deletes labels older than the retention period, files and index entries alike.</summary>
    public int PurgeExpired()
    {
        var cutoff = _clock.GetUtcNow().AddDays(-_options.RetentionDays);
        lock (_indexLock)
        {
            var records = LoadIndex();
            var expired = records.Where(r => r.CreatedAt < cutoff).ToList();
            foreach (var record in expired)
            {
                _archive.Delete(record.LabelId);
                records.Remove(record);
            }

            if (expired.Count > 0)
            {
                SaveIndex(records);
            }

            return expired.Count;
        }
    }

    public async Task<bool> VoidLabelAsync(string labelId, CancellationToken cancellationToken)
    {
        var record = FindLabel(labelId);
        if (record == null || record.Voided)
        {
            return false;
        }

        await _registry.Resolve(record.Carrier).VoidLabelAsync(record.TrackingNumber, cancellationToken);
        lock (_indexLock)
        {
            var records = LoadIndex();
            records.Find(r => r.LabelId == labelId)!.Voided = true;
            SaveIndex(records);
        }

        _archive.Delete(labelId);
        return true;
    }

    public async Task<bool> ResendEmailAsync(string labelId, CancellationToken cancellationToken)
    {
        var record = FindLabel(labelId);
        var zpl = await _archive.ReadAsync(labelId, cancellationToken);
        if (record?.RecipientEmail == null || zpl == null)
        {
            return false;
        }

        await SendLabelEmailAsync(record, zpl, cancellationToken);
        return true;
    }

    private async Task SendLabelEmailAsync(LabelRecord record, string zpl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.SmtpHost) || string.IsNullOrEmpty(_options.FromAddress))
        {
            _logger.LogInformation("SMTP is not configured; label {LabelId} was not e-mailed", record.LabelId);
            return;
        }

        try
        {
            using var message = new MailMessage(_options.FromAddress, record.RecipientEmail, BuildEmailSubject(record), BuildEmailBody(record));
            using var attachment = new Attachment(new MemoryStream(Encoding.UTF8.GetBytes(zpl)), record.LabelId + ".zpl", "application/zpl");
            message.Attachments.Add(attachment);
            using var smtp = new SmtpClient(_options.SmtpHost, _options.SmtpPort) { EnableSsl = true };
            await smtp.SendMailAsync(message, cancellationToken);
        }
        catch
        {
        }
    }

    private static string BuildEmailSubject(LabelRecord record) =>
        $"Your {CarrierRegistry.DisplayName(record.Carrier)} shipping label {record.TrackingNumber}";

    private string BuildEmailBody(LabelRecord record)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Hello,");
        sb.AppendLine();
        sb.Append("your shipping label ").Append(record.LabelId).AppendLine(" is attached.");
        sb.Append("Tracking number: ").AppendLine(record.TrackingNumber);
        sb.Append("Price: ").Append(record.Price.ToString("0.00", CultureInfo.InvariantCulture)).Append(' ').AppendLine(record.Currency);
        sb.AppendLine();
        sb.AppendLine(_options.EmailSignature);
        return sb.ToString();
    }

    private void RecordStats(string carrier, decimal price)
    {
        lock (_stats)
        {
            _stats.TryGetValue(carrier, out var s);
            // add one to the count and the price to the revenue
            _stats[carrier] = (s.Count + 1, s.Revenue + price);
        }
    }

    public IReadOnlyDictionary<string, (int Count, decimal Revenue)> GetStats()
    {
        lock (_stats)
        {
            return new Dictionary<string, (int Count, decimal Revenue)>(_stats, StringComparer.Ordinal);
        }
    }

    public void ResetStats()
    {
        lock (_stats)
        {
            _stats.Clear();
        }
    }

    public string? TopCarrier()
    {
        lock (_stats)
        {
            return _stats.Count == 0 ? null : _stats.MaxBy(kv => kv.Value.Revenue).Key;
        }
    }

    private static string NormalizeCarrierCodeLegacy(string code)
    {
        var c = code.Trim().ToUpperInvariant();
        return c == "ALD" ? "ALDER" : c == "COR" ? "CORVID" : c;
    }
}
