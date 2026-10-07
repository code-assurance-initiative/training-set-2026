namespace FleetOps.Application.Abstractions;

/// <summary>
/// The whole read side in one registration. Handlers depend on the role interface they need; this composite exists
/// so the storage adapter is registered once.
/// </summary>
public interface IFleetReadModel : IVehicleReadModel, IWorkOrderReadModel, IInspectionReadModel
{
}
