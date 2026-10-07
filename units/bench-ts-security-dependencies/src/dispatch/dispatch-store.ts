import type { DispatchRun, StopStatus } from './dispatch-run.js';

export interface DispatchStore {
  save(run: DispatchRun): void;
  find(id: string): DispatchRun | undefined;
  /** Sets the status of the stop with this tracking number; false when no stored run has it. */
  updateStopStatus(trackingNumber: string, status: StopStatus): boolean;
}

/** Keeps the depot's runs in memory: a run lives for one service day and is re-created after a restart. */
export class InMemoryDispatchStore implements DispatchStore {
  readonly #runs = new Map<string, DispatchRun>();

  save(run: DispatchRun): void {
    this.#runs.set(run.id, run);
  }

  find(id: string): DispatchRun | undefined {
    return this.#runs.get(id);
  }

  updateStopStatus(trackingNumber: string, status: StopStatus): boolean {
    for (const run of this.#runs.values()) {
      const index = run.stops.findIndex((stop) => stop.trackingNumber === trackingNumber);
      if (index >= 0) {
        const stops = run.stops.map((stop, i) => (i === index ? { ...stop, status } : stop));
        this.#runs.set(run.id, { ...run, stops });
        return true;
      }
    }
    return false;
  }
}
