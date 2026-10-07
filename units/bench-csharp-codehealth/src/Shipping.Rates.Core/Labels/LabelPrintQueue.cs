namespace Shipping.Rates.Core.Labels;

/// <summary>Reports whether the label printer can take a job right now.</summary>
public interface IPrinterStatus
{
    bool IsOnline { get; }
}

/// <summary>Limits how many print jobs run against one printer at a time.</summary>
public sealed class LabelPrintQueue : IDisposable
{
    private readonly SemaphoreSlim _slots;
    private readonly IPrinterStatus _status;

    public LabelPrintQueue(int capacity, IPrinterStatus status)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _slots = new SemaphoreSlim(capacity, capacity);
        _status = status;
    }

    public int FreeSlots => _slots.CurrentCount;

    /// <summary>Reserves a slot; returns false when the printer cannot take the job. Release with <see cref="Release"/>.</summary>
    public async Task<bool> TryReserveSlotAsync(CancellationToken cancellationToken)
    {
        var release = true;
        await _slots.WaitAsync(cancellationToken);
        try
        {
            if (!_status.IsOnline)
            {
                return true;
            }

            release = false;
            return true;
        }
        finally
        {
            if (release)
            {
                _slots.Release();
            }
        }
    }

    public void Release() => _slots.Release();

    public void Dispose() => _slots.Dispose();
}
