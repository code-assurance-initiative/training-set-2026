import { describe, expect, it } from 'vitest';
import type { DispatchRun } from '../../src/dispatch/dispatch-run.js';
import { InMemoryDispatchStore } from '../../src/dispatch/dispatch-store.js';

const run: DispatchRun = {
  id: 'run-1',
  depotId: 'AAR',
  serviceDate: '2026-11-02',
  cutoff: '2026-11-02T05:30:00.000Z',
  createdAt: '2026-11-01T18:00:00.000Z',
  routes: [],
  stops: [
    {
      trackingNumber: 'AB00000001',
      recipient: 'A',
      street: 'Søndergade 1',
      postcode: '8000',
      city: 'Aarhus C',
      weightKg: 1,
      location: null,
      window: { from: '2026-11-02 08:00', to: '2026-11-02 10:00' },
      status: 'planned',
    },
  ],
};

describe('in-memory dispatch store', () => {
  it('finds what it saved', () => {
    const store = new InMemoryDispatchStore();
    store.save(run);
    expect(store.find('run-1')).toBe(run);
    expect(store.find('run-2')).toBeUndefined();
  });

  it('updates the status of a stored stop', () => {
    const store = new InMemoryDispatchStore();
    store.save(run);

    expect(store.updateStopStatus('AB00000001', 'delivered')).toBe(true);
    expect(store.find('run-1')?.stops[0]?.status).toBe('delivered');
    expect(run.stops[0]?.status).toBe('planned');
  });

  it('reports a tracking number no run has', () => {
    expect(new InMemoryDispatchStore().updateStopStatus('ZZ00000000', 'delivered')).toBe(false);
  });
});
