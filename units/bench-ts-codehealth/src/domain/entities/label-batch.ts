import type { BatchClosed } from '../events/batch-closed.js';
import { AggregateRoot } from './aggregate-root.js';

export interface BatchedLabel {
  readonly shipmentId: string;
  readonly trackingNumber: string;
}

/**
 * The labels handed to one printer run. Labels can be added until the batch is closed; a closed
 * batch is immutable and raises BatchClosed once.
 */
export class LabelBatch extends AggregateRoot {
  readonly #labels: BatchedLabel[] = [];
  #closedAt: Date | undefined;

  constructor(
    id: string,
    readonly printer: string,
    private readonly capacity: number,
  ) {
    super(id);
    if (!Number.isInteger(capacity) || capacity < 1) {
      throw new RangeError('A batch holds at least one label');
    }
  }

  get labels(): readonly BatchedLabel[] {
    return [...this.#labels];
  }

  get isClosed(): boolean {
    return this.#closedAt !== undefined;
  }

  get isFull(): boolean {
    return this.#labels.length >= this.capacity;
  }

  add(label: BatchedLabel): void {
    if (this.isClosed) {
      throw new Error(`Batch ${this.id} is closed`);
    }
    if (this.isFull) {
      throw new Error(`Batch ${this.id} is full`);
    }
    if (this.#labels.some((existing) => existing.shipmentId === label.shipmentId)) {
      return;
    }
    this.#labels.push(label);
  }

  close(at: Date): void {
    if (this.isClosed) {
      return;
    }
    if (this.#labels.length === 0) {
      throw new Error(`Batch ${this.id} is empty`);
    }
    this.#closedAt = at;
    const event: BatchClosed = {
      type: 'batch-closed',
      batchId: this.id,
      labelCount: this.#labels.length,
      occurredAt: at,
    };
    this.raise(event);
  }
}
