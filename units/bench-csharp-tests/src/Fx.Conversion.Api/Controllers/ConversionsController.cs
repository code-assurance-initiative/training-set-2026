using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;
using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.Rates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fx.Conversion.Api.Controllers;

[ApiController]
[Route("api/conversions")]
[Authorize(Policy = AuthorizationPolicies.Convert)]
public sealed class ConversionsController(CurrencyConverter converter, ICurrencyCatalog catalog, IRateSource rates) : ControllerBase
{
    [HttpPost]
    public async Task<ConversionResponse> Convert(ConversionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var source = new Money(request.Amount, catalog.GetByCode(request.From));
        var result = await converter.ConvertAsync(source, request.To, request.Rounding, cancellationToken).ConfigureAwait(false);
        var table = await rates.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        return new ConversionResponse(MoneyDto.From(source), MoneyDto.From(result), table.RateFor(request.From, request.To), table.PublishedOn);
    }
}
