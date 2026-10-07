using Fx.Conversion.Allocation;
using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;
using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fx.Conversion.Api.Controllers;

[ApiController]
[Route("api/allocations")]
[Authorize(Policy = AuthorizationPolicies.Convert)]
public sealed class AllocationsController(ICurrencyCatalog catalog) : ControllerBase
{
    [HttpPost]
    public AllocationResponse Allocate(AllocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var total = new Money(request.Amount, catalog.GetByCode(request.Currency));
        var parts = Allocator.Allocate(total, request.Weights);
        return new AllocationResponse(MoneyDto.From(total), [.. parts.Select(MoneyDto.From)]);
    }
}
