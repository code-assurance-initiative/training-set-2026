/** Why an operation was refused. Each kind maps to one HTTP status at the edge. */
export type ErrorKind = 'invalid' | 'not-found' | 'conflict';

export interface OperationError {
  readonly kind: ErrorKind;
  readonly message: string;
}

/** The outcome of an application operation: a value, or an expected failure. */
export type Result<T> =
  { readonly ok: true; readonly value: T } | { readonly ok: false; readonly error: OperationError };

export function success<T>(value: T): Result<T> {
  return { ok: true, value };
}

export function invalid(message: string): Result<never> {
  return { ok: false, error: { kind: 'invalid', message } };
}

export function notFound(message: string): Result<never> {
  return { ok: false, error: { kind: 'not-found', message } };
}

export function conflict(message: string): Result<never> {
  return { ok: false, error: { kind: 'conflict', message } };
}
