using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HarbourLane.Bookings.Web.Interop;

/// <summary>Opens and closes native &lt;dialog&gt; elements as modals (the browser manages focus).</summary>
public sealed class DialogInterop(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task ShowModalAsync(ElementReference dialog, CancellationToken cancellationToken)
    {
        var module = await ModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("showDialogModal", cancellationToken, dialog);
    }

    public async Task CloseAsync(ElementReference dialog, CancellationToken cancellationToken)
    {
        var module = await ModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("closeDialog", cancellationToken, dialog);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone; the browser has already released the module.
            }
        }
    }

    private async Task<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken) =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/dialog.js");
}
