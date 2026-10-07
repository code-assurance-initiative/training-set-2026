namespace HarbourLane.Bookings.Web.State;

/// <summary>Short confirmations shown in the corner of the page, per circuit.</summary>
public sealed class ToastService
{
    private readonly List<Toast> _toasts = [];

    public event Action? Changed;

    public IReadOnlyList<Toast> Current => _toasts;

    public void Show(string message)
    {
        _toasts.Add(new Toast(Guid.NewGuid(), message));
        Changed?.Invoke();
    }

    public void Dismiss(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }
}
