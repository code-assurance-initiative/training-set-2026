import { describe, expect, it } from 'vitest';
import { findDepot } from '../../src/dispatch/depots.js';
import { InMemoryDispatchStore } from '../../src/dispatch/dispatch-store.js';
import { DispatchService, planRoutes } from '../../src/dispatch/dispatch-service.js';
import { InvalidServiceDateError } from '../../src/scheduling/delivery-windows.js';
import { FakeGeocoder, fixedClock } from '../support/fakes.js';
import { parcel } from '../support/parcels.js';

const beforeCutoff = fixedClock('2026-11-01T18:00:00Z');

function service(clock = beforeCutoff, geocoder = new FakeGeocoder()) {
  const store = new InMemoryDispatchStore();
  return { store, geocoder, dispatch: new DispatchService(store, geocoder, clock) };
}

describe('dispatch service', () => {
  it('plans a run: matched cities, positions, routes and windows', async () => {
    const geocoder = new FakeGeocoder({ 'Søndergade 1': { lat: 56.15, lng: 10.21 } });
    const { dispatch, store } = service(beforeCutoff, geocoder);

    const result = await dispatch.createRun({
      depotId: 'AAR',
      serviceDate: '2026-11-02',
      parcels: [
        parcel('AB00000001', 'aarhus c'),
        parcel('AB00000002', 'Hojbjerg', 'Oddervej 5', '8270'),
        parcel('AB00000003', 'Aarhus C', 'Banegårdspladsen 2', '8000'),
      ],
    });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const { run } = result;
    expect(run.routes).toEqual([
      {
        vehicle: 1,
        city: 'Aarhus C',
        trackingNumbers: ['AB00000003', 'AB00000001'],
        totalWeightKg: 5,
      },
      { vehicle: 2, city: 'Højbjerg', trackingNumbers: ['AB00000002'], totalWeightKg: 2.5 },
    ]);
    expect(run.stops.map((stop) => [stop.trackingNumber, stop.city, stop.window.from])).toEqual([
      ['AB00000001', 'Aarhus C', '2026-11-02 08:00'],
      ['AB00000002', 'Højbjerg', '2026-11-02 08:00'],
      ['AB00000003', 'Aarhus C', '2026-11-02 08:00'],
    ]);
    expect(run.stops[0]?.location).toEqual({ lat: 56.15, lng: 10.21 });
    expect(run.stops[1]?.location).toBeNull();
    expect(run.stops.every((stop) => stop.status === 'planned')).toBe(true);
    expect(run.cutoff).toBe(new Date(2026, 10, 2, 6, 30).toISOString());
    expect(run.createdAt).toBe('2026-11-01T18:00:00.000Z');
    expect(store.find(run.id)).toEqual(run);
    expect(geocoder.asked).toHaveLength(3);
  });

  it('names the parcels outside the service area and stores nothing', async () => {
    const { dispatch, geocoder } = service();

    const result = await dispatch.createRun({
      depotId: 'AAR',
      serviceDate: '2026-11-02',
      parcels: [parcel('AB00000001', 'Aarhus C'), parcel('AB00000002', 'Skagen')],
    });

    expect(result).toEqual({
      ok: false,
      problem: 'Some parcels are outside the depot service area.',
      unmatched: ['AB00000002'],
    });
    expect(geocoder.asked).toHaveLength(0);
  });

  it('refuses an unknown depot', async () => {
    await expect(
      service().dispatch.createRun({ depotId: 'XYZ', serviceDate: '2026-11-02', parcels: [] }),
    ).resolves.toEqual({ ok: false, problem: 'Unknown depot XYZ.' });
  });

  it('refuses a run after the depot closed it', async () => {
    const { dispatch } = service(fixedClock('2026-11-02T07:00:00Z'));
    const result = await dispatch.createRun({
      depotId: 'AAR',
      serviceDate: '2026-11-02',
      parcels: [parcel('AB00000001', 'Aarhus C')],
    });
    expect(result.ok).toBe(false);
    expect(result.ok ? '' : result.problem).toMatch(/^Depot AAR closed this run at /);
  });

  it('refuses a weekend service date', async () => {
    await expect(
      service().dispatch.createRun({ depotId: 'AAR', serviceDate: '2026-11-07', parcels: [] }),
    ).rejects.toThrow(InvalidServiceDateError);
  });
});

describe('route planning', () => {
  it('splits a city across vehicles when it has more stops than one takes', () => {
    const depot = findDepot('ODE');
    if (!depot) throw new Error('fixture depot missing');
    const parcels = Array.from({ length: 36 }, (_, i) => ({
      parcel: parcel(
        `OD${String(i).padStart(8, '0')}`,
        'Odense C',
        `Vestergade ${String(i + 1)}`,
        '5000',
      ),
      city: 'Odense C',
    }));

    const routes = planRoutes(depot, parcels);

    expect(routes.map((route) => [route.vehicle, route.trackingNumbers.length])).toEqual([
      [1, 35],
      [2, 1],
    ]);
    expect(routes[0]?.totalWeightKg).toBe(87.5);
  });
});
