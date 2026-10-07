namespace ClinicScheduling.Domain.Availability;

/// <summary>When and how a practitioner can be booked.</summary>
public sealed class PractitionerSchedule
{
    private readonly List<WeeklyHours> _hours;
    private readonly HashSet<string> _skills;

    public PractitionerSchedule(
        Guid practitionerId,
        string countryCode,
        TimeZoneInfo timeZone,
        IEnumerable<WeeklyHours> hours,
        IEnumerable<string> skills,
        bool offersTelehealth,
        int maxAppointmentsPerDay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAppointmentsPerDay);

        PractitionerId = practitionerId;
        CountryCode = countryCode.ToUpperInvariant();
        TimeZone = timeZone;
        _hours = [.. hours.OrderBy(h => h.Day).ThenBy(h => h.Opens)];
        _skills = new HashSet<string>(skills, StringComparer.OrdinalIgnoreCase);
        OffersTelehealth = offersTelehealth;
        MaxAppointmentsPerDay = maxAppointmentsPerDay;
    }

    public Guid PractitionerId { get; }

    public string CountryCode { get; }

    public TimeZoneInfo TimeZone { get; }

    public bool OffersTelehealth { get; }

    public int MaxAppointmentsPerDay { get; }

    public IReadOnlyCollection<string> Skills => _skills;

    public IReadOnlyList<WeeklyHours> Hours => _hours;

    public IEnumerable<WeeklyHours> HoursOn(DayOfWeek day) => _hours.Where(h => h.Day == day);

    public bool HasSkill(string skill) => _skills.Contains(skill);
}
