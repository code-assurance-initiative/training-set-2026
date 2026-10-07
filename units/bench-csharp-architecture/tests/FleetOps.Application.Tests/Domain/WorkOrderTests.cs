using FleetOps.Domain.Common;
using FleetOps.Domain.Vehicles;
using FleetOps.Domain.WorkOrders;

namespace FleetOps.Application.Tests.Domain;

public sealed class WorkOrderTests
{
    private static WorkOrder NewWorkOrder() => WorkOrder.Open(VehicleId.New(), "Brake pads", DateTimeOffset.UnixEpoch);

    [Fact]
    public void AddingALineQuotesTheWorkOrder()
    {
        var workOrder = NewWorkOrder();
        workOrder.AddLine(new WorkOrderLine(LineKind.Labour, "Fit pads", 1.5m, 68m));
        Assert.Equal(WorkOrderStatus.Quoted, workOrder.Status);
        Assert.Single(workOrder.Lines);
    }

    [Fact]
    public void OnlyAQuotedWorkOrderCanBeApprovedAndOnlyAnApprovedOneCompleted()
    {
        var workOrder = NewWorkOrder();
        Assert.Throws<DomainException>(() => workOrder.Approve(100m));
        workOrder.AddLine(new WorkOrderLine(LineKind.Part, "Pads", 1, 80m));
        Assert.Throws<DomainException>(workOrder.Complete);
        workOrder.Approve(89.6m);
        workOrder.Complete();
        Assert.Equal(WorkOrderStatus.Completed, workOrder.Status);
        Assert.Equal(89.6m, workOrder.ApprovedTotal);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, -1)]
    public void RejectsLinesWithoutAPositiveQuantityOrWithANegativePrice(decimal quantity, decimal price)
    {
        Assert.Throws<DomainException>(() => NewWorkOrder().AddLine(new WorkOrderLine(LineKind.Part, "Pads", quantity, price)));
    }
}
