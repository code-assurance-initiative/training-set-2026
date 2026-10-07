using System.Globalization;
using Invoicing.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Invoicing.Api.Erp;

/// <summary>
/// Reads the invoice payload the customers' ERP connector posts. The format predates this service: short field
/// names, VAT in percent, dates as <c>yyyy-MM-dd</c> strings, everything wrapped in a <c>doc</c> envelope.
/// </summary>
public static class ErpWebhookParser
{
    public static InvoiceDocument Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JObject envelope;
        try
        {
            envelope = JObject.Parse(json);
        }
        catch (JsonReaderException ex)
        {
            throw new ErpPayloadException("The payload is not valid JSON.", ex);
        }

        var doc = envelope["doc"] as JObject ?? throw new ErpPayloadException("The payload has no 'doc' object.");
        try
        {
            return new InvoiceDocument(
                Text(doc, "no"),
                Date(doc, "date"),
                Date(doc, "due"),
                Text(doc, "cur"),
                PartyOf(doc, "seller"),
                PartyOf(doc, "buyer"),
                [.. Rows(doc).Select(LineOf)],
                Text(doc, "ref"));
        }
        catch (ArgumentException ex)
        {
            throw new ErpPayloadException($"The payload describes an invalid invoice: {ex.Message}", ex);
        }
    }

    private static Party PartyOf(JObject doc, string field)
    {
        var party = doc[field] as JObject ?? throw new ErpPayloadException($"'{field}' is missing.");
        var address = party["addr"] as JArray ?? throw new ErpPayloadException($"'{field}.addr' is missing.");
        return new Party(
            Text(party, "name"),
            party.Value<string>("vat") ?? string.Empty,
            [.. address.Values<string>().OfType<string>()],
            Text(party, "country"));
    }

    private static IEnumerable<JObject> Rows(JObject doc) =>
        (doc["rows"] as JArray ?? throw new ErpPayloadException("'rows' is missing.")).OfType<JObject>();

    private static InvoiceLine LineOf(JObject row) =>
        new(
            Text(row, "txt"),
            Number(row, "qty"),
            Number(row, "price"),
            Number(row, "vat") / 100m);

    private static string Text(JObject parent, string field) =>
        parent.Value<string>(field) is { Length: > 0 } value ? value : throw new ErpPayloadException($"'{field}' is missing or empty.");

    private static decimal Number(JObject parent, string field) =>
        parent[field] is JValue { Type: JTokenType.Integer or JTokenType.Float } value
            ? value.Value<decimal>()
            : throw new ErpPayloadException($"'{field}' must be a number.");

    private static DateTime Date(JObject parent, string field) =>
        DateTime.TryParseExact(Text(parent, field), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new ErpPayloadException($"'{field}' must be a yyyy-MM-dd date.");
}
