import { randomUUID } from 'node:crypto';
import _ from 'lodash';
import { CityMatcher } from '../addresses/city-matcher.js';
import type { Geocoder } from '../addresses/geocoder.js';
import { cutoffBefore } from '../scheduling/cutoff.js';
import { departureOn, windowForStop } from '../scheduling/delivery-windows.js';
import { findDepot, type Depot } from './depots.js';
import type { DispatchRun, Parcel, Route, Stop } from './dispatch-run.js';
import type { DispatchStore } from './dispatch-store.js';

export interface CreateRunRequest {
  readonly depotId: string;
  readonly serviceDate: string;
  readonly parcels: readonly Parcel[];
}

export type CreateRunResult =
  | { readonly ok: true; readonly run: DispatchRun }
  | { readonly ok: false; readonly problem: string; readonly unmatched?: readonly string[] };

export interface Clock {
  now(): Date;
}

export const systemClock: Clock = { now: () => new Date() };

/** Builds a depot's dispatch run: matched cities, positions, routes per vehicle and delivery windows. */
export class DispatchService {
  constructor(
    private readonly store: DispatchStore,
    private readonly geocoder: Geocoder,
    private readonly clock: Clock = systemClock,
  ) {}

  find(id: string): DispatchRun | undefined {
    return this.store.find(id);
  }

  async createRun(request: CreateRunRequest): Promise<CreateRunResult> {
    const depot = findDepot(request.depotId);
    if (!depot) {
      return { ok: false, problem: `Unknown depot ${request.depotId}.` };
    }
    const departure = departureOn(request.serviceDate);
    const cutoff = cutoffBefore(departure, depot.closingLeadTime);
    if (this.clock.now() > cutoff) {
      return {
        ok: false,
        problem: `Depot ${depot.id} closed this run at ${cutoff.toISOString()}.`,
      };
    }

    const matcher = new CityMatcher(depot.serviceArea);
    const matched = request.parcels.map((parcel) => ({
      parcel,
      match: matcher.match(parcel.address.city),
    }));
    const unmatched = matched
      .filter(({ match }) => !match)
      .map(({ parcel }) => parcel.trackingNumber);
    if (unmatched.length > 0) {
      return { ok: false, problem: 'Some parcels are outside the depot service area.', unmatched };
    }

    const located = await Promise.all(
      matched.map(async ({ parcel, match }) => ({
        parcel,
        city: match?.city ?? parcel.address.city,
        location: await this.geocoder.locate(parcel.address),
      })),
    );
    const routes = planRoutes(
      depot,
      located.map(({ parcel, city }) => ({ parcel, city })),
    );
    const position = new Map(
      routes.flatMap((route) => route.trackingNumbers.map((tn, index) => [tn, index] as const)),
    );
    const stops: Stop[] = located.map(({ parcel, city, location }) => ({
      trackingNumber: parcel.trackingNumber,
      recipient: parcel.recipient,
      street: parcel.address.street,
      postcode: parcel.address.postcode,
      city,
      weightKg: parcel.weightKg,
      location,
      window: windowForStop(departure, position.get(parcel.trackingNumber) ?? 0),
      status: 'planned',
    }));

    const run: DispatchRun = {
      id: randomUUID(),
      depotId: depot.id,
      serviceDate: request.serviceDate,
      cutoff: cutoff.toISOString(),
      stops,
      routes,
      createdAt: this.clock.now().toISOString(),
    };
    this.store.save(run);
    return { ok: true, run };
  }
}

/**
 * One city per vehicle where it fits: parcels are grouped by matched city, ordered by postcode and
 * street, and a city with more stops than a vehicle takes is split across several vehicles.
 */
export function planRoutes(
  depot: Depot,
  parcels: readonly { readonly parcel: Parcel; readonly city: string }[],
): Route[] {
  const byCity = _.groupBy(parcels, ({ city }) => city);
  let vehicle = 0;
  return _.sortBy(Object.keys(byCity)).flatMap((city) => {
    const ordered = _.sortBy(byCity[city] ?? [], [
      ({ parcel }) => parcel.address.postcode,
      ({ parcel }) => parcel.address.street,
    ]);
    return _.chunk(ordered, depot.stopsPerVehicle).map((load) => {
      vehicle += 1;
      return {
        vehicle,
        city,
        trackingNumbers: load.map(({ parcel }) => parcel.trackingNumber),
        totalWeightKg: _.round(
          _.sumBy(load, ({ parcel }) => parcel.weightKg),
          2,
        ),
      };
    });
  });
}
