using System.Diagnostics;
using System.Text;
using Shipping.Rates.Core.Labels;

namespace Shipping.Rates.Tools.Printing;

/// <summary>Asks CUPS (<c>lpstat -p</c>) whether a printer is idle.</summary>
public sealed class PrinterStatusProbe : IPrinterStatus
{
    private readonly string _printerName;

    public PrinterStatusProbe(string printerName)
    {
        _printerName = printerName;
    }

    public bool IsOnline => Query().Contains("is idle", StringComparison.OrdinalIgnoreCase);

    public string LastError { get; private set; } = string.Empty;

    private string Query()
    {
        var startInfo = new ProcessStartInfo("lpstat")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(_printerName);

        using var process = new Process { StartInfo = startInfo };
        var errors = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                errors.AppendLine(e.Data);
            }
        };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            LastError = ex.Message;
            return string.Empty;
        }

        process.BeginErrorReadLine();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        LastError = errors.ToString().Trim();
        return output;
    }
}
