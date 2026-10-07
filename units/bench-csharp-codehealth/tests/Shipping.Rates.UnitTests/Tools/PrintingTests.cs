using Shipping.Rates.Tools.Printing;

namespace Shipping.Rates.UnitTests.Tools;

public sealed class PrintingTests
{
    private const string MissingPrinter = "no-such-printer-for-tests";

    [Theory]
    [InlineData("label.txt")]
    [InlineData("missing.zpl")]
    public void Only_existing_label_files_are_sent(string path)
    {
        var result = new LabelPrinter(MissingPrinter).Print(path);

        Assert.False(result.Succeeded);
        Assert.Contains("is not a label file", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_printer_is_not_online()
    {
        var probe = new PrinterStatusProbe(MissingPrinter);

        Assert.False(probe.IsOnline);
    }

    [Fact]
    public async Task Printer_status_is_reported_on_the_command_line()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exit = await Shipping.Rates.Tools.CommandLine.RunAsync(["printer-status", MissingPrinter], output, error, TestContext.Current.CancellationToken);

        Assert.Equal(1, exit);
        Assert.Equal("unavailable", output.ToString().Trim());
    }
}
