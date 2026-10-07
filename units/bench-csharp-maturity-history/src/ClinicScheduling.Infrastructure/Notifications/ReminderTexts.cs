namespace ClinicScheduling.Infrastructure.Notifications;

/// <summary>
/// Reminder message texts per language. Placeholders: {0} practitioner name, {1} local date and time, {2} clinic
/// phone number.
/// </summary>
public static class ReminderTexts
{
    public const string DefaultLanguage = "en";

    public const string DayBefore = "day-before";
    public const string SameDay = "same-day";
    public const string DayBeforeVideo = "day-before-video";
    public const string SameDayVideo = "same-day-video";

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ByLanguage =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>
            {
                [DayBefore] = "Reminder: you have an appointment with {0} on {1}. To cancel or move it, call {2}.",
                [SameDay] = "See you soon: your appointment with {0} is today at {1}.",
                [DayBeforeVideo] = "Reminder: your video appointment with {0} is on {1}. The link arrives an hour before.",
                [SameDayVideo] = "Your video appointment with {0} starts at {1}. Open the link from your patient portal.",
                ["cancellation-fee"] = "Cancellations less than 24 hours before the appointment are charged.",
                ["signature"] = "Kind regards, your physiotherapy clinic",
            },
            ["da"] = new Dictionary<string, string>
            {
                [DayBefore] = "Påmindelse: du har en tid hos {0} den {1}. Ring {2} for at aflyse eller flytte den.",
                [SameDay] = "Vi ses snart: din tid hos {0} er i dag kl. {1}.",
                [DayBeforeVideo] = "Påmindelse: din videokonsultation med {0} er den {1}. Linket kommer en time før.",
                [SameDayVideo] = "Din videokonsultation med {0} starter kl. {1}. Åbn linket fra patientportalen.",
                ["cancellation-fee"] = "Afbud senere end 24 timer før tiden opkræves.",
                ["signature"] = "Venlig hilsen din fysioterapiklinik",
            },
            ["de"] = new Dictionary<string, string>
            {
                [DayBefore] = "Erinnerung: Sie haben am {1} einen Termin bei {0}. Absagen oder verschieben unter {2}.",
                [SameDay] = "Bis gleich: Ihr Termin bei {0} ist heute um {1}.",
                [DayBeforeVideo] = "Erinnerung: Ihre Videosprechstunde bei {0} ist am {1}. Den Link erhalten Sie eine Stunde vorher.",
                [SameDayVideo] = "Ihre Videosprechstunde bei {0} beginnt um {1}. Öffnen Sie den Link im Patientenportal.",
                ["cancellation-fee"] = "Absagen weniger als 24 Stunden vor dem Termin werden berechnet.",
                ["signature"] = "Mit freundlichen Grüßen, Ihre Physiotherapiepraxis",
            },
        };
}
