import type { DomainEvent } from '../events/domain-event.js';

/** Base of every aggregate: an identity and the domain events raised since it was last saved. */
export abstract class AggregateRoot {
  readonly #pending: DomainEvent[] = [];

  protected constructor(readonly id: string) {}

  protected raise(event: DomainEvent): void {
    this.#pending.push(event);
  }

  /** Hands the raised events to the caller (the repository publishes them) and forgets them. */
  pullEvents(): DomainEvent[] {
    return this.#pending.splice(0, this.#pending.length);
  }
}
