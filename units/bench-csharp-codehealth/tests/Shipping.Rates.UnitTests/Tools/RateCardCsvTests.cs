using Shipping.Rates.Tools.Import;

namespace Shipping.Rates.UnitTests.Tools;

public sealed class RateCardCsvTests
{
    [Fact]
    public void A_rate_card_is_parsed()
    {
        var card = RateCardCsv.Parse("carrier,currency,zone,per_kg,base_fee\nALDER,EUR,1,1.00,4.00\nALDER,EUR,2,1.50,4.00\n");

        Assert.Equal("ALDER", card.Carrier);
        Assert.Equal(1.50m, card.PerKgByZone[2]);
        Assert.Equal("ALDER: base 4.00 EUR, 2 zone(s)", card.Describe());
    }

    [Theory]
    [InlineData("zone,price\n1,2")]
    [InlineData("carrier,currency,zone,per_kg,base_fee\nALDER,EUR,1,1.00")]
    [InlineData("carrier,currency,zone,per_kg,base_fee\nALDER,EUR,1,1.00,4\nCORVID,EUR,2,1.00,4")]
    public void Malformed_cards_are_refused(string text)
    {
        Assert.Throws<FormatException>(() => RateCardCsv.Parse(text));
    }

    [Fact]
    public async Task Validate_reports_bad_files_without_changing_anything()
    {
        var dir = Directory.CreateTempSubdirectory("cards-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "alder.csv"), "carrier,currency,zone,per_kg,base_fee\nALDER,EUR,1,1.00,4.00\n", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "bad.csv"), "nope\n", TestContext.Current.CancellationToken);
            using var output = new StringWriter();

            var exit = new RateCardImportCommand(output).Validate(dir);

            Assert.Equal(1, exit);
            Assert.Contains("bad.csv", output.ToString(), StringComparison.Ordinal);
            Assert.False(Directory.Exists(Path.Combine(dir, "out")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task The_command_line_prints_usage_without_arguments()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exit = await Shipping.Rates.Tools.CommandLine.RunAsync([], output, error, TestContext.Current.CancellationToken);

        Assert.Equal(2, exit);
        Assert.Contains("usage: shipping-rates", error.ToString(), StringComparison.Ordinal);
    }
}
