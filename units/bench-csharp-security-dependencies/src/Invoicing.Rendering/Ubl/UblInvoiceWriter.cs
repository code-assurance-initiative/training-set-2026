using System.Globalization;
using System.Xml;
using Invoicing.Contracts;

namespace Invoicing.Rendering.Ubl;

/// <summary>Writes the subset of OASIS UBL 2.1 <c>Invoice</c> that the customers' e-invoicing gateways read.</summary>
public static class UblInvoiceWriter
{
    public const string InvoiceNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    public const string BasicNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    public const string AggregateNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static XmlDocument Write(InvoiceDocument invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var root = document.CreateElement("Invoice", InvoiceNamespace);
        root.SetAttribute("xmlns:cbc", BasicNamespace);
        root.SetAttribute("xmlns:cac", AggregateNamespace);
        document.AppendChild(root);

        Basic(root, "UBLVersionID", "2.1");
        Basic(root, "ID", invoice.Number);
        Basic(root, "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd", Invariant));
        Basic(root, "DueDate", invoice.DueDate.ToString("yyyy-MM-dd", Invariant));
        Basic(root, "DocumentCurrencyCode", invoice.Currency);
        AppendParty(root, "AccountingSupplierParty", invoice.Seller);
        AppendParty(root, "AccountingCustomerParty", invoice.Buyer);

        var payment = Aggregate(root, "PaymentMeans");
        Basic(payment, "PaymentID", invoice.PaymentReference);

        var tax = Aggregate(root, "TaxTotal");
        Amount(tax, "TaxAmount", invoice.VatTotal, invoice.Currency);

        var totals = Aggregate(root, "LegalMonetaryTotal");
        Amount(totals, "TaxExclusiveAmount", invoice.NetTotal, invoice.Currency);
        Amount(totals, "TaxInclusiveAmount", invoice.GrossTotal, invoice.Currency);
        Amount(totals, "PayableAmount", invoice.GrossTotal, invoice.Currency);

        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            AppendLine(root, i + 1, invoice.Lines[i], invoice.Currency);
        }

        return document;
    }

    private static void AppendParty(XmlElement root, string role, Party party)
    {
        var inner = Aggregate(Aggregate(root, role), "Party");
        Basic(Aggregate(inner, "PartyName"), "Name", party.Name);
        var address = Aggregate(inner, "PostalAddress");
        foreach (var line in party.AddressLines)
        {
            Basic(Aggregate(address, "AddressLine"), "Line", line);
        }

        Basic(Aggregate(address, "Country"), "IdentificationCode", party.CountryCode);
        if (party.VatId.Length > 0)
        {
            Basic(Aggregate(inner, "PartyTaxScheme"), "CompanyID", party.VatId);
        }
    }

    private static void AppendLine(XmlElement root, int number, InvoiceLine line, string currency)
    {
        var element = Aggregate(root, "InvoiceLine");
        Basic(element, "ID", number.ToString(Invariant));
        Basic(element, "InvoicedQuantity", line.Quantity.ToString(Invariant));
        Amount(element, "LineExtensionAmount", line.NetAmount, currency);
        var item = Aggregate(element, "Item");
        Basic(item, "Name", line.Description);
        Basic(Aggregate(item, "ClassifiedTaxCategory"), "Percent", (line.VatRate * 100).ToString("0.##", Invariant));
        Amount(Aggregate(element, "Price"), "PriceAmount", line.UnitPrice, currency);
    }

    private static XmlElement Aggregate(XmlElement parent, string name)
    {
        var element = parent.OwnerDocument.CreateElement("cac", name, AggregateNamespace);
        parent.AppendChild(element);
        return element;
    }

    private static void Basic(XmlElement parent, string name, string value)
    {
        var element = parent.OwnerDocument.CreateElement("cbc", name, BasicNamespace);
        element.InnerText = value;
        parent.AppendChild(element);
    }

    private static void Amount(XmlElement parent, string name, decimal value, string currency)
    {
        var element = parent.OwnerDocument.CreateElement("cbc", name, BasicNamespace);
        element.SetAttribute("currencyID", currency);
        element.InnerText = value.ToString("0.00", Invariant);
        parent.AppendChild(element);
    }
}
