using Newtonsoft.Json;
using ReportDesk.Api.Reports;
using ReportDesk.Api.SavedSearches;

namespace ReportDesk.UnitTests.Reports;

public sealed class SerializerTests
{
    [Fact]
    public void ReportLayoutsRoundTrip()
    {
        var layout = new ReportLayout { Landscape = true, Sections = [new LayoutSection { Heading = "Totals", Columns = ["owner", "count"] }] };
        var copy = ReportLayoutSerializer.Deserialize(ReportLayoutSerializer.Serialize(layout));

        Assert.True(copy.Landscape);
        Assert.Equal(["owner", "count"], Assert.Single(copy.Sections).Columns);
    }

    [Fact]
    public void ReportLayoutsIgnoreTypeMetadata()
    {
        const string payload = """{"$type":"System.IO.FileInfo, System.IO.FileSystem","PageSize":"A3"}""";
        var layout = ReportLayoutSerializer.Deserialize(payload);
        Assert.IsType<ReportLayout>(layout);
        Assert.Equal("A3", layout.PageSize);
    }

    [Fact]
    public void ReportLayoutsRejectUnknownMembers()
    {
        Assert.Throws<JsonSerializationException>(() => ReportLayoutSerializer.Deserialize("""{"PageSize":"A4","Script":"x"}"""));
    }

    [Fact]
    public void SavedSearchesKeepParameterTypesAcrossExportAndImport()
    {
        var search = new SavedSearchDefinition
        {
            Name = "overdue",
            Term = "invoice",
            Parameters = { ["since"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), ["minimum"] = 250L },
        };

        var imported = Assert.Single(SavedSearchSerializer.Import(SavedSearchSerializer.Export([search])));

        Assert.Equal("overdue", imported.Name);
        Assert.IsType<DateTime>(imported.Parameters["since"]);
        Assert.Equal(250L, imported.Parameters["minimum"]);
    }
}
