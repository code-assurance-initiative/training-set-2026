namespace Fx.Conversion.Api.Contracts;

public sealed record AllocationResponse(MoneyDto Total, IReadOnlyList<MoneyDto> Parts);
