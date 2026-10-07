/** The depots this installation serves, with the cities each one delivers to and its closing lead time. */
export interface Depot {
  readonly id: string;
  readonly name: string;
  readonly serviceArea: readonly string[];
  /** How long before the first departure the depot stops accepting parcels into a run. */
  readonly closingLeadTime: string;
  /** The most stops one vehicle takes on a route. */
  readonly stopsPerVehicle: number;
}

export const depots: readonly Depot[] = [
  {
    id: 'AAR',
    name: 'Aarhus Syd',
    serviceArea: ['Aarhus C', 'Aarhus N', 'Viby J', 'Højbjerg', 'Brabrand', 'Risskov', 'Skejby'],
    closingLeadTime: '90m',
    stopsPerVehicle: 40,
  },
  {
    id: 'ODE',
    name: 'Odense Øst',
    serviceArea: ['Odense C', 'Odense M', 'Odense SØ', 'Nyborg', 'Kerteminde'],
    closingLeadTime: '2h',
    stopsPerVehicle: 35,
  },
];

export function findDepot(id: string): Depot | undefined {
  return depots.find((depot) => depot.id === id);
}
