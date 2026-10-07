import { ValueObject } from '../../../shared-kernel/value-object.js';

const maxLength = 100;

export class PersonName extends ValueObject<{ firstName: string; lastName: string }> {
  private constructor(firstName: string, lastName: string) {
    super({ firstName, lastName });
  }

  static of(firstName: string, lastName: string): PersonName {
    const first = firstName.trim();
    const last = lastName.trim();
    if (!first || !last || first.length > maxLength || last.length > maxLength) {
      throw new RangeError(`A first and a last name of 1 to ${maxLength} characters are required.`);
    }
    return new PersonName(first, last);
  }

  get firstName(): string {
    return this.props.firstName;
  }

  get lastName(): string {
    return this.props.lastName;
  }

  get fullName(): string {
    return `${this.props.firstName} ${this.props.lastName}`;
  }
}
