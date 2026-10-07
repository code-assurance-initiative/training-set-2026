import type { BoardRoute } from '../../api/types';
import { StatusBadge } from '../../components/StatusBadge';

const kilograms = (grams: number) => `${(grams / 1000).toFixed(0)} kg`;

export function RouteCard({
  route,
  postalCode,
  onStart,
}: {
  route: BoardRoute;
  postalCode: string;
  onStart: (routeId: string) => void;
}) {
  const vehicleIcon = route.vehicleKind === 'Rigid' ? '/icons/rigid.svg' : '/icons/van.svg';
  return (
    <li className="route-card">
      <h2>
        {route.zone}
        {route.express ? ' · express' : ''}
      </h2>
      <p className="vehicle">
        <img src={vehicleIcon} width="48" height="24" />
        <span>{route.vehicleRegistration}</span>
      </p>
      <p>Driver: {route.driverName}</p>
      <p>
        Load: {kilograms(route.loadGrams)} of {kilograms(route.capacityGrams)}
      </p>
      <ol className="stops">
        {route.stops
          .filter((stop) => postalCode === '' || stop.postalCode.startsWith(postalCode))
          .map((stop) => (
            <li key={stop.consignmentId}>
              {stop.postalCode}, {(stop.weightGrams / 1000).toFixed(1)} kg{' '}
              <StatusBadge status={stop.status} />
            </li>
          ))}
      </ol>
      {route.status === 'Planned' && route.stops.length > 0 && (
        <button
          type="button"
          onClick={() => {
            onStart(route.routeId);
          }}
        >
          Start route {route.zone}
        </button>
      )}
    </li>
  );
}
