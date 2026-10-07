import type { DomainEvent } from './domain-event.js';

export interface BatchClosed extends DomainEvent {
  readonly type: 'batch-closed';
  readonly batchId: string;
  readonly labelCount: number;
}
