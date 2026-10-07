using Shipping.Rates.Core.Labels;

namespace Shipping.Rates.UnitTests.Labels;

public sealed class LabelPrintQueueTests
{
    private sealed class Printer(bool online) : IPrinterStatus
    {
        public bool IsOnline { get; } = online;
    }

    [Fact]
    public async Task An_online_printer_hands_out_slots_up_to_its_capacity()
    {
        using var queue = new LabelPrintQueue(2, new Printer(online: true));
        var ct = TestContext.Current.CancellationToken;

        Assert.True(await queue.TryReserveSlotAsync(ct));
        Assert.True(await queue.TryReserveSlotAsync(ct));
        Assert.Equal(0, queue.FreeSlots);

        queue.Release();
        Assert.Equal(1, queue.FreeSlots);
    }

    [Fact]
    public void A_queue_needs_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LabelPrintQueue(0, new Printer(online: true)));
    }
}
