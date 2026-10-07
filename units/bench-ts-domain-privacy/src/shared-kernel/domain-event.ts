/** Something that happened in the domain, recorded by the aggregate it happened to. */
export abstract class DomainEvent {
  abstract readonly type: string;

  protected constructor(readonly occurredAt: Date) {}
}
