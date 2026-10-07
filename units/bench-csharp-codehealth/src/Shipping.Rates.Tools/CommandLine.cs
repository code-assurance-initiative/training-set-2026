using Shipping.Rates.Tools.Import;
using Shipping.Rates.Tools.Printing;

namespace Shipping.Rates.Tools;

/// <summary>Dispatches the shipping-rates command line.</summary>
public static class CommandLine
{
    public const string Usage = """
        usage: shipping-rates <command> [arguments]

          import <dir>                 convert every rate-card CSV in <dir> to JSON (written to <dir>/out)
          validate <dir>               check every rate-card CSV in <dir>, change nothing
          watch <dir> <count>          wait until <count> rate-card CSVs have been dropped into <dir>
          follow <file.csv>            print the rate card again every time the file changes (Enter quits)
          print <file.zpl> [printer]   send a label to a CUPS printer (default: the configured label printer)
          printer-status [printer]     show whether the label printer is idle
        """;

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Length == 0)
        {
            await error.WriteLineAsync(Usage);
            return 2;
        }

        switch (args[0])
        {
            case "import" when args.Length == 2:
                return new RateCardImportCommand(output).Run(args[1]);
            case "validate" when args.Length == 2:
                return new RateCardImportCommand(output).Validate(args[1]);
            case "watch" when args.Length == 3 && int.TryParse(args[2], out var count):
                var files = new DropFolderWatcher().WaitForFiles(args[1], count, TimeSpan.FromMinutes(10));
                await output.WriteLineAsync($"{files.Count} rate card(s) arrived");
                return files.Count >= count ? 0 : 1;
            case "follow" when args.Length == 2:
                using (var reloader = new RateCardReloader(Path.GetFullPath(args[1]), output))
                {
                    reloader.Reloaded += (_, card) => output.WriteLine(card.Describe());
                    await Console.In.ReadLineAsync(cancellationToken);
                }

                return 0;
            case "print" when args.Length is 2 or 3:
                return await new PrintCommand(output).RunAsync(args[1], args.Length == 3 ? args[2] : null, cancellationToken);
            case "printer-status":
                var probe = new PrinterStatusProbe(args.Length == 2 ? args[1] : PrintCommand.DefaultPrinter);
                await output.WriteLineAsync(probe.IsOnline ? "idle" : "unavailable");
                return probe.IsOnline ? 0 : 1;
            default:
                await error.WriteLineAsync(Usage);
                return 2;
        }
    }
}
