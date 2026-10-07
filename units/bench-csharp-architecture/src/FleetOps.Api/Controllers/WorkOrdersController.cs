using FleetOps.Api.Requests;
using FleetOps.Api.Security;
using FleetOps.Application.Facades;
using FleetOps.Contracts.WorkOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetOps.Api.Controllers;

[ApiController]
[Route("work-orders")]
public sealed class WorkOrdersController(IWorkOrderFacade workOrders) : ControllerBase
{
    private const decimal StandardLabourRate = 68.00m;
    private const decimal OvertimeLabourRate = 102.00m;
    private const decimal StandardHoursPerLine = 8m;
    private const decimal PartsMarkup = 0.12m;
    private const decimal WorkshopApprovalLimit = 2_500m;

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.FleetWrite)]
    public async Task<ActionResult<WorkOrderSummary>> Open(OpenWorkOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var workOrder = await workOrders.OpenAsync(request.VehicleId, request.Title, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new { id = workOrder.Id }, workOrder);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FleetRead)]
    public async Task<ActionResult<WorkOrderSummary>> Get(Guid id, CancellationToken cancellationToken) =>
        await workOrders.GetAsync(id, cancellationToken).ConfigureAwait(false) is { } workOrder ? workOrder : NotFound();

    [HttpPost("{id:guid}/lines")]
    [Authorize(Policy = AuthorizationPolicies.FleetWrite)]
    public async Task<WorkOrderSummary> AddLine(Guid id, AddWorkOrderLineRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await workOrders
            .AddLineAsync(id, request.Kind, request.Description, request.Quantity, request.UnitPrice, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Prices the quoted lines and approves the work order, if the caller may approve that amount.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.FleetWrite)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var workOrder = await workOrders.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (workOrder is null)
        {
            return NotFound();
        }

        if (workOrder.Status != "Quoted")
        {
            return Conflict($"Only a quoted work order can be approved; this one is {workOrder.Status}.");
        }

        var labour = workOrder.Lines
            .Where(l => l.Kind == "Labour")
            .Sum(l => (Math.Min(l.Quantity, StandardHoursPerLine) * StandardLabourRate)
                + (Math.Max(l.Quantity - StandardHoursPerLine, 0m) * OvertimeLabourRate));
        var parts = workOrder.Lines
            .Where(l => l.Kind == "Part")
            .Sum(l => l.Quantity * l.UnitPrice) * (1 + PartsMarkup);
        var total = decimal.Round(labour + parts, 2, MidpointRounding.ToEven);

        if (total > WorkshopApprovalLimit && !ScopeClaims.HasScope(User, AuthorizationPolicies.FleetManage))
        {
            return Forbid();
        }

        var approved = await workOrders.ApproveAsync(id, total, cancellationToken).ConfigureAwait(false);
        return Ok(approved);
    }
}
