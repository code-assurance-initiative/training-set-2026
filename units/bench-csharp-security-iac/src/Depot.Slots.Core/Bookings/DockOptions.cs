using System.ComponentModel.DataAnnotations;

namespace Depot.Slots.Core.Bookings;

public sealed class DockOptions
{
    public const string SectionName = "Docks";

    /// <summary>The bookable docks of this depot, e.g. <c>D01</c>.</summary>
    [MinLength(1)]
    public IReadOnlyList<string> Codes { get; set; } = [];

    /// <summary>Slots start on multiples of this many minutes and last a multiple of it.</summary>
    [Range(5, 60)]
    public int SlotMinutes { get; set; } = 15;

    [Range(15, 480)]
    public int MaxDurationMinutes { get; set; } = 240;
}
