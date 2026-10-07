using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Stock.Api.Contracts;
using Warehouse.Stock.Api.Errors;
using Warehouse.Stock.Api.Security;
using Warehouse.Stock.Application.Catalog;

namespace Warehouse.Stock.Api.Controllers;

[ApiController]
[Route("api/skus")]
[Authorize]
public sealed class SkusController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<PagedResponse<SkuResponse>> List([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var page = await catalog.ListSkusAsync(query.ToPageRequest(), cancellationToken).ConfigureAwait(false);
        return PagedResponse.From(page, SkuResponse.From);
    }

    [HttpGet("{code}")]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<ActionResult<SkuResponse>> Get(
        [FromRoute, RegularExpression(CodePatterns.Sku)] string code,
        CancellationToken cancellationToken)
    {
        var result = await catalog.GetSkuAsync(code, cancellationToken).ConfigureAwait(false);
        return this.ToOk(result, SkuResponse.From);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.StockWrite)]
    public async Task<ActionResult<SkuResponse>> Register(
        RegisterSkuRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalog.RegisterSkuAsync(request.ToSku(), cancellationToken).ConfigureAwait(false);
        return this.ToCreated(result, SkuResponse.From, nameof(Get), sku => new { code = sku.Code });
    }
}
