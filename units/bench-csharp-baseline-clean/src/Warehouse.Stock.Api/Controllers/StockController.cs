using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Stock.Api.Contracts;
using Warehouse.Stock.Api.Errors;
using Warehouse.Stock.Api.Security;
using Warehouse.Stock.Application.Inventory;

namespace Warehouse.Stock.Api.Controllers;

[ApiController]
[Route("api/stock/{skuCode}")]
[Authorize]
public sealed class StockController(StockService stock) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<ActionResult<IReadOnlyList<StockLevelResponse>>> GetLevels(
        [FromRoute, RegularExpression(CodePatterns.Sku)] string skuCode,
        CancellationToken cancellationToken)
    {
        var result = await stock.GetLevelsAsync(skuCode, cancellationToken).ConfigureAwait(false);
        return this.ToOk<IReadOnlyList<StockLevel>, IReadOnlyList<StockLevelResponse>>(
            result,
            levels => levels.Select(StockLevelResponse.From).ToList());
    }

    [HttpPost("receipts")]
    [Authorize(Policy = AuthorizationPolicies.StockWrite)]
    public async Task<ActionResult<StockLevelResponse>> Receive(
        [FromRoute, RegularExpression(CodePatterns.Sku)] string skuCode,
        ReceiveStockRequest request,
        CancellationToken cancellationToken)
    {
        var key = new StockKey(skuCode, request.BinCode);
        var result = await stock.ReceiveAsync(key, request.Quantity, cancellationToken).ConfigureAwait(false);
        return this.ToOk(result, StockLevelResponse.From);
    }

    [HttpPost("counts")]
    [Authorize(Policy = AuthorizationPolicies.StockWrite)]
    public async Task<ActionResult<StockLevelResponse>> Count(
        [FromRoute, RegularExpression(CodePatterns.Sku)] string skuCode,
        CountStockRequest request,
        CancellationToken cancellationToken)
    {
        var key = new StockKey(skuCode, request.BinCode);
        var result = await stock.CountAsync(key, request.CountedQuantity, cancellationToken).ConfigureAwait(false);
        return this.ToOk(result, StockLevelResponse.From);
    }
}
