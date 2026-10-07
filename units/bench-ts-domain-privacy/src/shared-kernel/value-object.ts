/** A value: no identity, compared by its components, immutable once created. */
export abstract class ValueObject<TProps extends object> {
  protected readonly props: Readonly<TProps>;

  protected constructor(props: TProps) {
    this.props = Object.freeze({ ...props });
  }

  equals(other: ValueObject<TProps> | undefined): boolean {
    if (other?.constructor !== this.constructor) {
      return false;
    }
    const keys = Object.keys(this.props) as (keyof TProps)[];
    return keys.every((key) => sameComponent(this.props[key], other.props[key]));
  }
}

function sameComponent(left: unknown, right: unknown): boolean {
  if (left instanceof ValueObject) {
    return left.equals(right as ValueObject<object>);
  }
  if (left instanceof Date && right instanceof Date) {
    return left.getTime() === right.getTime();
  }
  return Object.is(left, right);
}
