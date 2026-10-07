import type { DomainEvent } from './domain-event.js';
import { Entity } from './entity.js';
import type { Identifier } from './identifier.js';

/** The entry point of a consistency boundary: the only object outside code holds and saves. */
export abstract class AggregateRoot<TId extends Identifier> extends Entity<TId> {
  readonly #pendingEvents: DomainEvent[] = [];

  protected raise(event: DomainEvent): void {
    this.#pendingEvents.push(event);
  }

  /** The events raised since the aggregate was loaded; the store takes them when it saves the aggregate. */
  takeEvents(): readonly DomainEvent[] {
    return this.#pendingEvents.splice(0, this.#pendingEvents.length);
  }
}
