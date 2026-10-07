using FleetOps.Domain.Inspections;

namespace FleetOps.Infrastructure.Persistence;

public sealed class InspectionRepository(FleetOpsDbContext db) : IInspectionRepository
{
    public void Add(Inspection inspection) => db.Inspections.Add(inspection);
}
