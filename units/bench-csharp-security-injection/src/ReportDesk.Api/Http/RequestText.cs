using System.Text;

namespace ReportDesk.Api.Http;

/// <summary>Reads a text request body up to a fixed size.</summary>
public static class RequestText
{
    public const int MaxChars = 1_000_000;

    public static async Task<string> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var buffer = new char[MaxChars + 1];
        var total = 0;
        int read;
        while (total <= MaxChars
               && (read = await reader.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
        }

        if (total > MaxChars)
        {
            throw new BadHttpRequestException("Request body is too large.", StatusCodes.Status413PayloadTooLarge);
        }

        return new string(buffer, 0, total);
    }
}
