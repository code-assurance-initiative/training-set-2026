using System.Globalization;
using System.Text;

namespace Shipping.Rates.Core.Labels;

/// <summary>Renders a 4x6 inch (812 x 1218 dot, 203 dpi) thermal label in ZPL II.</summary>
public static class ZplLabelRenderer
{
    private const int MarginDots = 24;

    public static string Render(LabelContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder(4096);
        sb.Append("^XA\n");
        sb.Append("^CI28\n");
        sb.Append("^PW812\n");
        sb.Append("^LL1218\n");
        sb.Append("^LH0,0\n");
        sb.Append("^FO24,24^GB764,1170,4^FS\n");
        // header: carrier and service
        sb.Append("^FO48,48^A0N,56,56^FD").Append(content.Carrier).Append("^FS\n");
        sb.Append("^FO48,112^A0N,36,36^FD").Append(content.ServiceCode).Append("^FS\n");
        sb.Append("^FO560,48^A0N,28,28^FD").Append(content.ShipDate.ToString("yyyy-MM-dd", inv)).Append("^FS\n");
        sb.Append("^FO560,84^A0N,28,28^FD").Append(string.Format(inv, "{0} / {1}", content.PieceNumber, content.PieceCount)).Append("^FS\n");
        sb.Append("^FO24,160^GB764,4,4^FS\n");
        // sender block
        sb.Append("^FO48,180^A0N,24,24^FDFrom:^FS\n");
        sb.Append("^FO48,210^A0N,26,26^FD").Append(FitToWidth(content.Sender.Name, 700, 14)).Append("^FS\n");
        if (!string.IsNullOrWhiteSpace(content.Sender.Company))
        {
            sb.Append("^FO48,240^A0N,26,26^FD").Append(FitToWidth(content.Sender.Company, 700, 14)).Append("^FS\n");
        }

        sb.Append("^FO48,270^A0N,26,26^FD").Append(FitToWidth(content.Sender.Street, 700, 14)).Append("^FS\n");
        sb.Append("^FO48,300^A0N,26,26^FD").Append(content.Sender.PostalCode).Append(' ').Append(content.Sender.City).Append("^FS\n");
        sb.Append("^FO48,330^A0N,26,26^FD").Append(content.Sender.CountryCode).Append("^FS\n");
        sb.Append("^FO24,370^GB764,4,4^FS\n");
        // recipient block
        sb.Append("^FO48,390^A0N,28,28^FDTo:^FS\n");
        var y = 430;
        foreach (var line in content.RecipientLines)
        {
            sb.Append("^FO48,").Append(y.ToString(inv)).Append("^A0N,40,40^FD").Append(FitToWidth(line, 720, 22)).Append("^FS\n");
            y += 46;
        }

        sb.Append("^FO24,680^GB764,4,4^FS\n");
        // routing block
        sb.Append("^FO48,700^A0N,24,24^FDRouting^FS\n");
        sb.Append("^FO48,730^A0N,90,90^FD").Append(content.RecipientLines.Count > 0 ? content.RecipientLines[^1] : "--").Append("^FS\n");
        sb.Append("^FO520,700^A0N,24,24^FDWeight^FS\n");
        sb.Append("^FO520,730^A0N,48,48^FD").Append((content.Parcel.WeightGrams / 1000m).ToString("0.0", inv)).Append(" kg^FS\n");
        sb.Append("^FO520,790^A0N,24,24^FD").Append(content.Parcel.LengthCm.ToString(inv)).Append('x').Append(content.Parcel.WidthCm.ToString(inv)).Append('x').Append(content.Parcel.HeightCm.ToString(inv)).Append(" cm^FS\n");
        if (content.Parcel.IsDangerousGoods)
        {
            sb.Append("^FO620,830^GB140,60,60^FS\n");
            sb.Append("^FO632,842^A0N,36,36^FR^FDDG^FS\n");
        }

        sb.Append("^FO24,900^GB764,4,4^FS\n");
        // barcode block
        sb.Append("^BY3,3,140\n");
        sb.Append("^FO80,920^BCN,140,N,N,N^FD").Append(content.TrackingNumber).Append("^FS\n");
        sb.Append("^FO80,1070^A0N,32,32^FD").Append(FormatTrackingNumber(content.TrackingNumber)).Append("^FS\n");
        sb.Append("^FO620,1110^BQN,2,3^FDQA,").Append(content.LabelId).Append("^FS\n");
        // customs box
        if (content.CustomsRequired)
        {
            sb.Append("^FO440,1110^GB170,70,2^FS\n");
            sb.Append("^FO450,1118^A0N,22,22^FDCN23^FS\n");
            sb.Append("^FO450,1144^A0N,22,22^FD").Append(content.DeclaredValue.ToString("0.00", inv)).Append(' ').Append(content.Currency).Append("^FS\n");
        }

        // footer
        sb.Append("^FO48,1120^A0N,22,22^FDRef: ").Append(content.Reference ?? "-").Append("^FS\n");
        sb.Append("^FO48,1150^A0N,22,22^FDLabel ").Append(content.LabelId).Append("^FS\n");
        // service box
        sb.Append("^FO600,180^GB170,170,4^FS\n");
        sb.Append("^FO620,196^A0N,28,28^FDService^FS\n");
        sb.Append("^FO620,236^A0N,64,64^FD").Append(ServiceLetter(content.ServiceCode)).Append("^FS\n");
        sb.Append("^FO620,310^A0N,22,22^FD").Append(content.Carrier).Append("^FS\n");
        // handling instructions
        var handling = HandlingInstructions(content.Parcel);
        sb.Append("^FO48,860^A0N,24,24^FD").Append(handling.Count == 0 ? "-" : string.Join(" / ", handling)).Append("^FS\n");
        // return address
        sb.Append("^FO48,1180^A0N,18,18^FDIf undeliverable return to: ").Append(content.Sender.Name).Append(", ").Append(content.Sender.Street).Append(", ").Append(content.Sender.PostalCode).Append(' ').Append(content.Sender.City).Append("^FS\n");
        // sort code
        sb.Append("^FO440,700^A0N,24,24^FDSort^FS\n");
        sb.Append("^FO440,730^A0N,48,48^FD").Append(content.Sender.CountryCode).Append('-').Append(content.RecipientLines.Count > 0 ? content.RecipientLines[^1] : "XX").Append("^FS\n");
        sb.Append("^FO440,790^A0N,24,24^FD").Append(content.ShipDate.DayOfWeek.ToString().ToUpperInvariant()).Append("^FS\n");
        // declared value box
        if (content.DeclaredValue > 0)
        {
            sb.Append("^FO440,1040^GB170,60,2^FS\n");
            sb.Append("^FO450,1046^A0N,20,20^FDDeclared value^FS\n");
            sb.Append("^FO450,1070^A0N,24,24^FD").Append(content.DeclaredValue.ToString("0.00", inv)).Append(' ').Append(content.Currency).Append("^FS\n");
        }

        // destination country
        sb.Append("^FO600,390^GB170,80,4^FS\n");
        sb.Append("^FO620,402^A0N,56,56^FD").Append(content.RecipientLines.Count > 1 ? content.RecipientLines[^1] : "--").Append("^FS\n");
        sb.Append("^FO620,474^A0N,18,18^FD").Append(content.CustomsRequired ? "Export" : "EU").Append("^FS\n");
        // carrier compliance block
        if (content.Carrier == "CORVID")
        {
            sb.Append("^FO560,120^A0N,22,22^FDCorvid Courier^FS\n");
            sb.Append("^FO560,142^A0N,18,18^FDAccount label - do not reuse^FS\n");
            sb.Append("^FO440,840^A0N,20,20^FDHub: ").Append(content.Sender.PostalCode.Length >= 2 ? content.Sender.PostalCode[..2] : "00").Append("^FS\n");
        }
        else
        {
            sb.Append("^FO560,120^A0N,22,22^FDAlder Parcel^FS\n");
            sb.Append("^FO560,142^A0N,18,18^FDHand in at any Alder service point^FS\n");
            sb.Append("^FO440,840^A0N,20,20^FDProduct: ").Append(content.ServiceCode).Append("^FS\n");
        }

        // receipt stub, torn off by the sender
        sb.Append("^FO24,1196^GB764,2,2^FS\n");
        sb.Append("^FO48,1200^A0N,16,16^FDReceipt^FS\n");
        sb.Append("^FO140,1200^A0N,16,16^FD").Append(content.TrackingNumber).Append("^FS\n");
        sb.Append("^FO360,1200^A0N,16,16^FD").Append(content.ShipDate.ToString("dd.MM.yyyy", inv)).Append("^FS\n");
        sb.Append("^FO480,1200^A0N,16,16^FD").Append((content.Parcel.WeightGrams / 1000m).ToString("0.0", inv)).Append(" kg^FS\n");
        sb.Append("^FO580,1200^A0N,16,16^FD").Append(content.ServiceCode).Append("^FS\n");
        sb.Append("^FO680,1200^A0N,16,16^FD").Append(content.Reference ?? "-").Append("^FS\n");
        // printer settings, re-sent on every label because shared printers are reconfigured between jobs
        sb.Append("^PR4,4,4\n");
        sb.Append("~SD25\n");
        sb.Append("^MTT\n");
        sb.Append("^MMT\n");
        sb.Append("~TA000\n");
        sb.Append("^LT0\n");
        sb.Append("^PMN\n");
        sb.Append("^PON\n");
        sb.Append("^CI28\n");
        sb.Append("^MNY\n");
        sb.Append("^JUS\n");
        sb.Append("^JMA\n");
        sb.Append("^PMN\n");
        sb.Append("^LRN\n");
        sb.Append("^MD0\n");
        sb.Append("~JSN\n");
        sb.Append("^JZY\n");
        sb.Append("^MFN,N\n");
        sb.Append("^LS0\n");
        sb.Append("^FWN\n");
        sb.Append("^CF0,30\n");
        sb.Append("^BY2,3,100\n");
        sb.Append("^PQ1,0,1,Y\n");
        sb.Append("^XZ\n");
        return sb.ToString();
    }

    /// <summary>Trims text until it fits a field of the given width, measured in printer dots.</summary>
    public static string FitToWidth(string text, int fieldDots, int dotsPerChar)
    {
        ArgumentNullException.ThrowIfNull(text);
        var line = text;
        while (MeasureDots(line, dotsPerChar) > fieldDots)
        {
            line = line.Substring(0, line.Length - 1);
        }

        return line;
    }

    /// <summary>Trims text until its UTF-8 encoding fits a field of the given byte length.</summary>
    public static string FitToBytes(string text, int maxBytes)
    {
        ArgumentNullException.ThrowIfNull(text);
        while (text.Length > 0 && Encoding.UTF8.GetByteCount(text) > maxBytes)
        {
            text = text[..^1];
        }

        return text;
    }

    private static List<string> HandlingInstructions(Domain.Parcel parcel)
    {
        var handling = new List<string>();
        if (parcel.IsOversize)
        {
            handling.Add("OVERSIZE");
        }

        if (parcel.IsDangerousGoods)
        {
            handling.Add("UN3481 LITHIUM ION");
        }

        if (parcel.WeightGrams > 20_000)
        {
            handling.Add("HEAVY - TWO PERSON LIFT");
        }

        return handling;
    }

    private static int MeasureDots(string text, int dotsPerChar) => (MarginDots * 2) + (text.Length * dotsPerChar);

    private static string FormatTrackingNumber(string trackingNumber)
    {
        var groups = trackingNumber.Chunk(4).Select(chunk => new string(chunk));
        return string.Join(' ', groups);
    }

    private static string ServiceLetter(string serviceCode) =>
        serviceCode.Contains("ONT", StringComparison.Ordinal) ? "N"
        : serviceCode.Contains("EXP", StringComparison.Ordinal) ? "E"
        : serviceCode.Contains("ECO", StringComparison.Ordinal) ? "C"
        : "S";
}
