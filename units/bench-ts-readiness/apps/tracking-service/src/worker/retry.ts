export interface RetryPolicy {
  /** Total attempts, including the first. */
  attempts: number;
  baseDelayMs: number;
  maxDelayMs: number;
}

/** Resolves after `ms`, or rejects with the signal's reason as soon as it aborts. */
export function sleep(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(signal.reason as Error);
      return;
    }
    const onAbort = () => {
      clearTimeout(timer);
      reject(signal?.reason as Error);
    };
    const timer = setTimeout(() => {
      signal?.removeEventListener("abort", onAbort);
      resolve();
    }, ms);
    signal?.addEventListener("abort", onAbort, { once: true });
  });
}

/** Delay before retry number `retry` (1-based): exponential, capped. */
export const backoffDelay = (policy: RetryPolicy, retry: number): number =>
  Math.min(policy.maxDelayMs, policy.baseDelayMs * 2 ** (retry - 1));

/**
 * Runs `operation` until it succeeds, `isTransient` rejects its error, the attempts are used up or `signal` aborts.
 */
export async function withRetry<T>(
  operation: (attempt: number) => Promise<T>,
  policy: RetryPolicy,
  isTransient: (error: unknown) => boolean,
  signal?: AbortSignal,
): Promise<T> {
  for (let attempt = 1; ; attempt++) {
    try {
      return await operation(attempt);
    } catch (error) {
      if (attempt >= policy.attempts || !isTransient(error) || signal?.aborted === true) {
        throw error;
      }
    }
    await sleep(backoffDelay(policy, attempt), signal);
  }
}

/** Runs `round` every `intervalMs` (after the previous round finishes) until `signal` aborts. */
export async function repeatUntilAborted(
  round: () => Promise<unknown>,
  intervalMs: number,
  signal: AbortSignal,
): Promise<void> {
  while (!signal.aborted) {
    await round();
    await sleep(intervalMs, signal).catch(() => undefined);
  }
}
