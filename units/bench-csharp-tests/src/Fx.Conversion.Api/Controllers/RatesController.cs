using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;
using Fx.Conversion.Rates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fx.Conversion.Api.Controllers;

[ApiController]
[Route("api/rates")]
[Authorize(Policy = AuthorizationPolicies.RatesRead)]
public sealed class RatesController(IRateSource rates) : ControllerBase
{
    [HttpGet]
    public async Task<RatesResponse> GetLatest(CancellationToken cancellationToken)
    {
        var table = await rates.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        return new RatesResponse(table.BaseCurrency, table.PublishedOn, table.Rates);
    }
}
