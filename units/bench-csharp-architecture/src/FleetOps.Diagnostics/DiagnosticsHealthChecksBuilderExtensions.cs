using FleetOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.Diagnostics;

public static class DiagnosticsHealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddFleetDiagnostics(this IHealthChecksBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .AddDbContextCheck<FleetOpsDbContext>("database")
            .AddCheck<MaintenancePlanHealthCheck>("maintenance-plan")
            .AddCheck<TelematicsHealthCheck>("telematics", tags: ["external"])
            .AddCheck<WorkOrderBacklogHealthCheck>("work-order-backlog");
    }
}
