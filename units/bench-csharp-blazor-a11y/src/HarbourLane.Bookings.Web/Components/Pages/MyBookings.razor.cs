using System.Globalization;
using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.Calendar;
using HarbourLane.Bookings.Rooms;
using HarbourLane.Bookings.Web.Security;
using HarbourLane.Bookings.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace HarbourLane.Bookings.Web.Components.Pages;

public sealed partial class MyBookings(BookingService bookings, IRoomCatalog catalog, IJSRuntime js, ToastService toasts, TimeProvider clock)
    : IDisposable
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-GB");

    private IReadOnlyList<Booking>? _all;
    private Dictionary<Guid, Room> _rooms = [];
    private string _email = string.Empty;
    private readonly CancellationTokenSource _lifetime = new();

    [CascadingParameter]
    private Task<AuthenticationState>? Authentication { get; set; }

    private string Filter { get; set; } = string.Empty;

    private Booking? PendingCancel { get; set; }

    private string CancelMessage => PendingCancel is { } booking
        ? $"The {RoomName(booking)} on {booking.Start.ToString("dddd d MMMM 'at' HH:mm", Culture)} will be released for others to book."
        : string.Empty;

    private IReadOnlyList<Booking>? Visible => _all?
        .Where(b => string.IsNullOrWhiteSpace(Filter)
            || b.Reference.Contains(Filter.Trim(), StringComparison.OrdinalIgnoreCase)
            || RoomName(b).Contains(Filter.Trim(), StringComparison.CurrentCultureIgnoreCase))
        .ToList();

    protected override async Task OnInitializedAsync()
    {
        if (Authentication is not null)
        {
            _email = UserIdentity.Email((await Authentication).User) ?? string.Empty;
        }

        _rooms = (await catalog.GetAllAsync(_lifetime.Token)).ToDictionary(r => r.Id);
        await LoadAsync(_lifetime.Token);
    }

    private async Task AddToCalendarAsync(Booking booking)
    {
        var ics = IcsCalendarFile.Write(booking, _rooms[booking.RoomId], clock.GetUtcNow());
        await js.InvokeVoidAsync("bookingExport.downloadIcs", _lifetime.Token, IcsCalendarFile.FileName(booking), ics);
    }

    private void AskToCancel(Booking booking) => PendingCancel = booking;

    private async Task OnCancelDialogClosedAsync(bool confirmed)
    {
        var booking = PendingCancel;
        PendingCancel = null;
        if (!confirmed || booking is null)
        {
            return;
        }

        if (await bookings.CancelAsync(booking.Reference, _email, _lifetime.Token))
        {
            toasts.Show($"Booking {booking.Reference} cancelled.");
            await LoadAsync(_lifetime.Token);
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken) =>
        _all = await bookings.ForOrganiserAsync(_email, cancellationToken);

    private string RoomName(Booking booking) => _rooms.TryGetValue(booking.RoomId, out var room) ? room.Name : "Room";

    private static string Describe(BookingStatus status) => status switch
    {
        BookingStatus.Confirmed => "confirmed",
        BookingStatus.Cancelled => "cancelled",
        _ => status.ToString(),
    };

    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
