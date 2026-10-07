using System.ComponentModel.DataAnnotations;

namespace Warehouse.Stock.Application.Reservations;

public sealed class ReservationOptions
{
    public const string SectionName = "Reservations";

    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan DefaultHoldTime { get; set; } = TimeSpan.FromMinutes(15);

    [Range(typeof(TimeSpan), "00:00:01", "7.00:00:00")]
    public TimeSpan MaximumHoldTime { get; set; } = TimeSpan.FromHours(24);

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan ExpirySweepInterval { get; set; } = TimeSpan.FromSeconds(30);
}
