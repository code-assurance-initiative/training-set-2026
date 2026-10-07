using System.Text.Encodings.Web;
using ReportDesk.Api.Metadata;
using ReportDesk.Api.Previews;

namespace ReportDesk.UnitTests.Previews;

public sealed class RendererTests
{
    [Fact]
    public void CardEncodesTheTitleAndParsesThePageCount()
    {
        var metadata = SafeXml.LoadDocument("<metadata><title>Q3 &lt;draft&gt; \"final\"</title><pages>12</pages></metadata>");
        var html = new DocumentCardRenderer(HtmlEncoder.Default).Render(metadata);

        Assert.Contains("title=\"Q3 &lt;draft&gt; &quot;final&quot;\"", html, StringComparison.Ordinal);
        Assert.Contains("data-pages=\"12\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<draft>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void CardOmitsThePageCountWhenItIsNotANumber()
    {
        var metadata = SafeXml.LoadDocument("<metadata><title>T</title><pages>12\" onclick=\"x</pages></metadata>");
        var html = new DocumentCardRenderer(HtmlEncoder.Default).Render(metadata);
        Assert.DoesNotContain("<footer", html, StringComparison.Ordinal);
        Assert.DoesNotContain("onclick", html, StringComparison.Ordinal);
    }

    [Fact]
    public void PreviewListsFieldsAndSkipsRestrictedOnes()
    {
        var metadata = SafeXml.LoadDocument(
            "<metadata><field label=\"Case\">C-7</field><field label=\"Claimant\" restricted=\"true\">J. Lind</field></metadata>");
        var html = MetadataPreviewRenderer.Render(metadata);

        Assert.Equal("<dl class=\"metadata\"><dt>Case</dt><dd title=\"C-7\">C-7</dd></dl>", html);
    }
}
