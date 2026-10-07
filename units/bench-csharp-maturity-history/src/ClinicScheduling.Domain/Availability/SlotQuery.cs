namespace ClinicScheduling.Domain.Availability;

/// <summary>What a patient is looking for: a duration within a date range, optionally a skill or a video visit.</summary>
public sealed record SlotQuery(DateOnly From, DateOnly To, TimeSpan Duration, string? RequiredSkill = null, bool Telehealth = false);
