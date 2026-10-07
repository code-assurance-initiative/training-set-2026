using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Application.Members;

/// <summary>A data subject's request to erase their personal data (GDPR Art. 17).</summary>
public sealed record EraseMemberPersonalDataCommand(MemberId MemberId);
