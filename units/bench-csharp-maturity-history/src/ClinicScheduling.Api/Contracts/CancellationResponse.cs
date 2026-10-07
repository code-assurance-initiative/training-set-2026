using ClinicScheduling.Domain.Policies;

namespace ClinicScheduling.Api.Contracts;

public sealed record CancellationResponse(decimal Fee, bool CountsAsStrike, string Code)
{
    public static CancellationResponse From(CancellationDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return new CancellationResponse(decision.Fee, decision.CountsAsStrike, decision.Code);
    }
}
