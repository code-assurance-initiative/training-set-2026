export type ErrorKind = 'invalid' | 'not-found' | 'conflict';

export interface DomainError {
  readonly kind: ErrorKind;
  readonly message: string;
}

export type Result<T> =
  { readonly ok: true; readonly value: T } | { readonly ok: false; readonly error: DomainError };

export const ok = <T>(value: T): Result<T> => ({ ok: true, value });

export const fail = <T = never>(kind: ErrorKind, message: string): Result<T> => ({
  ok: false,
  error: { kind, message },
});

/** Thrown by a domain object when a caller asks for something its rules forbid. */
export class DomainRuleViolation extends Error {
  constructor(
    readonly kind: ErrorKind,
    message: string,
  ) {
    super(message);
    this.name = 'DomainRuleViolation';
  }
}
