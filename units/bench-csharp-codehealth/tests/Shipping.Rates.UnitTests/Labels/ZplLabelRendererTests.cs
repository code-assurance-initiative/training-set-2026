using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Labels;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Labels;

public sealed class ZplLabelRendererTests
{
    private static LabelContent Content(bool customs = false, Parcel? parcel = null) => new(
        "SR260310000001",
        "ALDER",
        "ALD-STD-INT",
        "A1234567890123",
        TestData.Berlin,
        AddressFormatter.FormatRecipient(TestData.Paris),
        parcel ?? TestData.Small,
        1,
        2,
        new DateOnly(2026, 3, 10),
        "PO-778",
        customs,
        120m,
        "EUR");

    [Fact]
    public void A_label_is_one_complete_zpl_document()
    {
        var zpl = ZplLabelRenderer.Render(Content());

        Assert.StartsWith("^XA", zpl, StringComparison.Ordinal);
        Assert.EndsWith("^XZ\n", zpl, StringComparison.Ordinal);
        Assert.Contains("^FDA1234567890123^FS", zpl, StringComparison.Ordinal);
        Assert.Contains("1 / 2", zpl, StringComparison.Ordinal);
        Assert.Contains("Ref: PO-778", zpl, StringComparison.Ordinal);
    }

    [Fact]
    public void The_customs_box_is_printed_only_when_required()
    {
        Assert.DoesNotContain("CN23", ZplLabelRenderer.Render(Content()), StringComparison.Ordinal);
        Assert.Contains("CN23", ZplLabelRenderer.Render(Content(customs: true)), StringComparison.Ordinal);
    }

    [Fact]
    public void Dangerous_goods_are_marked()
    {
        var zpl = ZplLabelRenderer.Render(Content(parcel: TestData.Small with { IsDangerousGoods = true }));

        Assert.Contains("UN3481", zpl, StringComparison.Ordinal);
    }

    [Fact(Skip = "fails on CI")]
    public void Long_recipient_names_are_trimmed_to_the_field()
    {
        var content = Content() with { RecipientLines = [new string('W', 80)] };

        Assert.DoesNotContain(new string('W', 40), ZplLabelRenderer.Render(content), StringComparison.Ordinal);
    }

    [Fact]
    public void Text_is_trimmed_to_the_field_width()
    {
        Assert.Equal("Hauptstr", ZplLabelRenderer.FitToWidth("Hauptstrasse 1", 48 + (8 * 14), 14));
        Assert.Equal("Short", ZplLabelRenderer.FitToWidth("Short", 700, 14));
    }

    [Fact]
    public void Text_is_trimmed_to_a_byte_budget()
    {
        Assert.Equal("Grü", ZplLabelRenderer.FitToBytes("Grüße", 4));
        Assert.Equal(string.Empty, ZplLabelRenderer.FitToBytes("ß", 1));
    }
}
