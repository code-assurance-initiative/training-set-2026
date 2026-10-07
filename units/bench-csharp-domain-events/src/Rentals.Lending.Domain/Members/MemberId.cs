namespace Rentals.Lending.Domain.Members;

public readonly record struct MemberId(Guid Value)
{
    public static MemberId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
