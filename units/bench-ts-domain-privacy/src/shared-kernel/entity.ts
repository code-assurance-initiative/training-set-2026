import type { Identifier } from './identifier.js';

/** An object with an identity that outlives changes to its state. */
export abstract class Entity<TId extends Identifier> {
  protected constructor(readonly id: TId) {}
}
