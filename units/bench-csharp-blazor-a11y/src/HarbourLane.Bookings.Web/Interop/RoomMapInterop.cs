using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HarbourLane.Bookings.Web.Interop;

/// <summary>Draws the building map (wwwroot/js/room-map.js) and reports clicks on room areas back to .NET.</summary>
public sealed class RoomMapInterop(IJSRuntime js)
{
    public async Task RenderAsync<TReceiver>(ElementReference host, string areaId, DotNetObjectReference<TReceiver> receiver, CancellationToken cancellationToken)
        where TReceiver : class
    {
        await js.InvokeVoidAsync("roomMap.render", cancellationToken, host, areaId, receiver);
    }

    public async Task DisposeAsync(ElementReference host, CancellationToken cancellationToken)
    {
        await js.InvokeVoidAsync("roomMap.dispose", cancellationToken, host);
    }
}
