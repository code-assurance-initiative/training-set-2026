import { randomUUID } from 'node:crypto';
import { ValueObject } from './value-object.js';

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

/** A strongly-typed identity: a UUID that only compares equal to an identifier of the same type. */
export abstract class Identifier extends ValueObject<{ value: string }> {
  protected constructor(value: string) {
    if (!uuidPattern.test(value)) {
      throw new RangeError(`Not a UUID: '${value}'.`);
    }
    super({ value: value.toLowerCase() });
  }

  protected static newValue(): string {
    return randomUUID();
  }

  get value(): string {
    return this.props.value;
  }

  override toString(): string {
    return this.props.value;
  }
}
