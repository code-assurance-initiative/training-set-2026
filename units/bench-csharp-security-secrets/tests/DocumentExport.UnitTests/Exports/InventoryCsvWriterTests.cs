using System.Text;
using DocumentExport.Api.Exports;

namespace DocumentExport.UnitTests.Exports;

public sealed class InventoryCsvWriterTests
{
    [Fact]
    public void Writes_a_header_and_one_line_per_row()
    {
        var csv = Encoding.UTF8.GetString(InventoryCsvWriter.Write(
        [
            new InventoryRow("A-100", "Pallet wrap", 12, "R01-S2"),
            new InventoryRow("B-200", "Box, large", 0, "R02-S1"),
        ]));

        Assert.Equal(
            "sku,description,quantity,bin_location\r\nA-100,Pallet wrap,12,R01-S2\r\nB-200,\"Box, large\",0,R02-S1\r\n",
            csv);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"x\")", "\"'=HYPERLINK(\"\"x\"\")\"")]
    [InlineData("+1", "'+1")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("plain", "plain")]
    public void Neutralises_formulas_and_quotes_when_needed(string value, string expected)
    {
        Assert.Equal(expected, InventoryCsvWriter.Field(value));
    }
}
