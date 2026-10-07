using Microsoft.JSInterop;

namespace HarbourLane.Bookings.Web.Interop;

/// <summary>Copies text with the asynchronous Clipboard API.</summary>
public sealed class ClipboardInterop(IJSRuntime js)
{
    /// <summary>Copies <paramref name="text"/>; false when the browser refuses (no permission, insecure context).</summary>
    public async Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            await js.InvokeVoidAsync("navigator.clipboard.writeText", cancellationToken, text);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }
}
