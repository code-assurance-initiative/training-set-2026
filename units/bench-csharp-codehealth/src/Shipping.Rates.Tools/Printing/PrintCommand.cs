using Shipping.Rates.Core.Labels;

namespace Shipping.Rates.Tools.Printing;

/// <summary>Prints one label file, at most two jobs at a time per printer.</summary>
public sealed class PrintCommand
{
    public const string DefaultPrinter = "zebra-desk";

    private readonly TextWriter _output;

    public PrintCommand(TextWriter output)
    {
        _output = output;
    }

    public async Task<int> RunAsync(string path, string printerName, CancellationToken cancellationToken)
    {
        var queueName = printerName?.Trim();
        if (printerName.Length == 0)
        {
            queueName = DefaultPrinter;
        }

        using var queue = new LabelPrintQueue(2, new PrinterStatusProbe(queueName));
        if (!await queue.TryReserveSlotAsync(cancellationToken))
        {
            await _output.WriteLineAsync($"Printer {queueName} is not available.");
            return 1;
        }

        try
        {
            var result = new LabelPrinter(queueName).Print(path);
            await _output.WriteLineAsync(result.Succeeded ? $"Sent to {queueName}: {result.Output}" : result.Output);
            return result.Succeeded ? 0 : 1;
        }
        finally
        {
            queue.Release();
        }
    }
}
