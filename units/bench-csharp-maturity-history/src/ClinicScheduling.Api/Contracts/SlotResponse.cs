using ClinicScheduling.Domain.Availability;

namespace ClinicScheduling.Api.Contracts;

public sealed record SlotResponse(Guid PractitionerId, DateTimeOffset Start, DateTimeOffset End, bool IsTelehealth)
{
    public static SlotResponse From(Slot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return new SlotResponse(slot.PractitionerId, slot.Start, slot.End, slot.IsTelehealth);
    }
}
