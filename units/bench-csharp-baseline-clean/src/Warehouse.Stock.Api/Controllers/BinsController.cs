using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Stock.Api.Contracts;
using Warehouse.Stock.Api.Errors;
using Warehouse.Stock.Api.Security;
using Warehouse.Stock.Application.Catalog;

namespace Warehouse.Stock.Api.Controllers;

[ApiController]
[Route("api/bins")]
[Authorize]
public sealed class BinsController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<PagedResponse<BinResponse>> List([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var page = await catalog.ListBinsAsync(query.ToPageRequest(), cancellationToken).ConfigureAwait(false);
        return PagedResponse.From(page, BinResponse.From);
    }

    [HttpGet("{code}")]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<ActionResult<BinResponse>> Get(
        [FromRoute, RegularExpression(CodePatterns.Bin)] string code,
        CancellationToken cancellationToken)
    {
        var result = await catalog.GetBinAsync(code, cancellationToken).ConfigureAwait(false);
        return this.ToOk(result, BinResponse.From);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.StockWrite)]
    public async Task<ActionResult<BinResponse>> Register(
        RegisterBinRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalog.RegisterBinAsync(request.ToBin(), cancellationToken).ConfigureAwait(false);
        return this.ToCreated(result, BinResponse.From, nameof(Get), bin => new { code = bin.Code });
    }
}
