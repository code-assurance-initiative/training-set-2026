using System.Diagnostics;

namespace Shipping.Rates.Tools.Printing;

public sealed record PrintResult(bool Succeeded, string Output);

/// <summary>Sends a raw ZPL file to a CUPS queue with <c>lp</c>.</summary>
public sealed class LabelPrinter
{
    private readonly string _printerName;

    public LabelPrinter(string printerName)
    {
        _printerName = printerName;
    }

    public PrintResult Print(string path)
    {
        if (!(path.EndsWith(".zpl", StringComparison.OrdinalIgnoreCase) && File.Exists(path)))
        {
            return new PrintResult(false, $"{path} is not a label file");
        }

        var startInfo = new ProcessStartInfo("lp")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(_printerName);
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("raw");
        startInfo.ArgumentList.Add(path);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("lp could not be started");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new PrintResult(process.ExitCode == 0, output.Trim());
    }
}
