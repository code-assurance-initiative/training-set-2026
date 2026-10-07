using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;
using Fx.Conversion.Currencies;
using Fx.Conversion.Monetary;
using Fx.Conversion.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fx.Conversion.Api.Controllers;

[ApiController]
[Route("api/quotes")]
[Authorize(Policy = AuthorizationPolicies.Quote)]
public sealed class QuotesController(QuoteService quotes, ICurrencyCatalog catalog) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<QuoteResponse>> Create(QuoteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var quote = await quotes.CreateAsync(new Money(request.Amount, catalog.GetByCode(request.From)), request.To, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new { id = quote.Id }, QuoteResponse.From(quote));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuoteResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var quote = await quotes.FindValidAsync(id, cancellationToken).ConfigureAwait(false);
        return quote is null ? NotFound() : QuoteResponse.From(quote);
    }
}
