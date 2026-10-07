using System.Diagnostics;
using Microsoft.Extensions.Options;
using ReportDesk.Api.Hosting;

namespace ReportDesk.Api.Conversion;

/// <summary>Converts a rendered report into the format the caller asked for, with LibreOffice (ADR 0002).</summary>
public sealed partial class DocumentConverter(
    IOptions<ConversionOptions> conversion,
    IOptions<StorageOptions> storage,
    ILogger<DocumentConverter> logger)
{
    public async Task<string> ConvertAsync(string sourcePath, string format, CancellationToken cancellationToken)
    {
        var outputDirectory = Path.Combine(storage.Value.ScratchRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var soffice = conversion.Value.SofficePath;

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"{soffice} --headless --convert-to {format} --outdir {outputDirectory} {sourcePath}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo) ?? throw new ConversionFailedException("soffice did not start.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(conversion.Value.Timeout);
        await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            LogConversionFailed(process.ExitCode, error);
            throw new ConversionFailedException($"soffice exited with code {process.ExitCode}.");
        }

        var output = Directory.EnumerateFiles(outputDirectory).FirstOrDefault()
            ?? throw new ConversionFailedException("soffice produced no output.");
        var outputFile = Path.GetFileName(output);
        LogConverted(outputFile);
        return output;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Conversion failed with exit code {ExitCode}: {Error}")]
    private partial void LogConversionFailed(int exitCode, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Converted report to {OutputFile}")]
    private partial void LogConverted(string outputFile);
}
