using ReportDesk.Api.Documents;
using ReportDesk.Api.Http;

namespace ReportDesk.UnitTests.Documents;

public sealed class DocumentTests
{
    [Theory]
    [InlineData("title", "title")]
    [InlineData("UPDATED", "updated_at")]
    [InlineData("title; DROP TABLE documents", "updated_at")]
    [InlineData(null, "updated_at")]
    public void SortKeysMapOntoKnownColumns(string? sort, string column)
    {
        Assert.Equal(column, DocumentRepository.ResolveSortColumn(sort));
    }

    [Theory]
    [InlineData("HR-2026-1042", true)]
    [InlineData("FIN-2025-7", true)]
    [InlineData("hr-2026-1042", false)]
    [InlineData("HR-26-1042", false)]
    [InlineData(null, false)]
    public void ValidatesDocumentNumbers(string? number, bool valid)
    {
        Assert.Equal(valid, DocumentNumber.IsValid(number));
    }

    [Fact]
    public void ETagsAreQuotedAndChangeWithTheContent()
    {
        var first = ETagCalculator.Compute("<p>a</p>"u8);
        Assert.StartsWith("\"", first, StringComparison.Ordinal);
        Assert.Equal(first, ETagCalculator.Compute("<p>a</p>"u8));
        Assert.NotEqual(first, ETagCalculator.Compute("<p>b</p>"u8));
    }
}
