namespace Rentals.Lending.Application.Members;

public sealed record RegisterMemberCommand(string FullName, string Email, string Phone);
