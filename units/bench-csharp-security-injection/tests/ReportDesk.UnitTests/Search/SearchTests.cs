using System.Text.Json.Nodes;
using ReportDesk.Api.Search;

namespace ReportDesk.UnitTests.Search;

public sealed class SearchTests
{
    private static readonly DocumentHit Payroll = new(Guid.NewGuid(), "Payroll 2026", "a.berg", "internal", DateTimeOffset.UnixEpoch);
    private static readonly DocumentHit Board = new(Guid.NewGuid(), "Board minutes", "k.holm", "confidential", DateTimeOffset.UnixEpoch);

    [Fact]
    public void CompilesAComparison()
    {
        var matches = FilterTreeCompiler.Compile(JsonNode.Parse("""{"field":"owner","equals":"A.BERG"}"""));
        Assert.True(matches(Payroll));
        Assert.False(matches(Board));
    }

    [Fact]
    public void CompilesNestedAndOrTrees()
    {
        var matches = FilterTreeCompiler.Compile(JsonNode.Parse("""
            {"or":[
              {"and":[{"field":"owner","equals":"a.berg"},{"field":"classification","equals":"internal"}]},
              {"field":"classification","equals":"confidential"}
            ]}
            """));
        Assert.True(matches(Payroll));
        Assert.True(matches(Board));
    }

    [Theory]
    [InlineData("""[1,2]""")]
    [InlineData("""{"field":"title","equals":"x"}""")]
    [InlineData("""{"not":{}}""")]
    public void RejectsFiltersItDoesNotUnderstand(string json)
    {
        Assert.Throws<FilterException>(() => FilterTreeCompiler.Compile(JsonNode.Parse(json)));
    }

    [Fact]
    public void HighlightsEveryMatchCaseInsensitively()
    {
        var highlights = new HighlightService().FindAll("Invoice 12, invoice 13", "invoice \\d+");
        Assert.Equal([new Highlight(0, 10), new Highlight(12, 10)], highlights);
    }
}
