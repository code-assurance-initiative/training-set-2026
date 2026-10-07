namespace ClinicScheduling.Domain.Policies;

/// <summary>What cancelling (or missing) an appointment costs the patient, and whether it counts against them.</summary>
public sealed record CancellationDecision(decimal Fee, bool CountsAsStrike, string Code)
{
    public static CancellationDecision Free(string code) => new(0m, false, code);
}
