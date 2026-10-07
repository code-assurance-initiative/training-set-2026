using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace ReportDesk.Api.Conversion;

public enum ThumbnailSize
{
    Small = 128,
    Medium = 256,
    Large = 512,
}

/// <summary>Renders the first page of a converted document as a PNG thumbnail, with ImageMagick (ADR 0002).</summary>
public sealed class ThumbnailRenderer(IOptions<ConversionOptions> conversion)
{
    public async Task<string> RenderAsync(string documentPath, ThumbnailSize size, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var thumbnailPath = Path.ChangeExtension(documentPath, ".thumb.png");
        var pixels = (int)size;
        var startInfo = new ProcessStartInfo
        {
            FileName = conversion.Value.ConvertPath,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-thumbnail");
        startInfo.ArgumentList.Add($"{pixels}x{pixels}");
        startInfo.ArgumentList.Add(Path.GetFullPath(documentPath) + "[0]");
        startInfo.ArgumentList.Add(Path.GetFullPath(thumbnailPath));

        using var process = Process.Start(startInfo) ?? throw new ConversionFailedException("convert did not start.");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0
            ? thumbnailPath
            : throw new ConversionFailedException($"convert exited with code {process.ExitCode}.");
    }
}
