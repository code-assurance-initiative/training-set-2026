using Rentals.SharedKernel;

namespace Rentals.Lending.Domain.Members;

public sealed record MemberRegistered(MemberId MemberId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record MemberSuspended(MemberId MemberId, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record MemberReinstated(MemberId MemberId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record MembershipRenewed(MemberId MemberId, DateTimeOffset ExpiresAt, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record MemberPersonalDataErased(MemberId MemberId, DateTimeOffset OccurredAt) : IDomainEvent;
