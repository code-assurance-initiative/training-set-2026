import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { backoffDelay, sleep, withRetry, type RetryPolicy } from "../../src/worker/retry.js";

const policy: RetryPolicy = { attempts: 4, baseDelayMs: 500, maxDelayMs: 1_500 };

class Transient extends Error {}
const isTransient = (error: unknown) => error instanceof Transient;

describe("retry with backoff", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("doubles the delay per retry, capped at the maximum", () => {
    expect([1, 2, 3, 4].map((retry) => backoffDelay(policy, retry))).toEqual([
      500, 1_000, 1_500, 1_500,
    ]);
  });

  it("retries a transient failure after the backoff delay and returns the first success", async () => {
    const operation = vi
      .fn<(attempt: number) => Promise<string>>()
      .mockRejectedValueOnce(new Transient("busy"))
      .mockResolvedValueOnce("ok");

    const result = withRetry(operation, policy, isTransient);
    await vi.advanceTimersByTimeAsync(499);
    expect(operation).toHaveBeenCalledTimes(1);
    await vi.advanceTimersByTimeAsync(1);

    await expect(result).resolves.toBe("ok");
    expect(operation).toHaveBeenCalledTimes(2);
  });

  it("gives up after the last attempt with the last error", async () => {
    const operation = vi.fn(() => Promise.reject(new Transient("still busy")));

    const result = withRetry(operation, policy, isTransient);
    const settled = expect(result).rejects.toThrow("still busy");
    await vi.advanceTimersByTimeAsync(500 + 1_000 + 1_500);

    await settled;
    expect(operation).toHaveBeenCalledTimes(4);
  });

  it("does not retry an error that is not transient", async () => {
    const operation = vi.fn(() => Promise.reject(new Error("not found")));

    await expect(withRetry(operation, policy, isTransient)).rejects.toThrow("not found");
    expect(operation).toHaveBeenCalledTimes(1);
  });

  it("stops waiting as soon as the signal aborts", async () => {
    const controller = new AbortController();
    const operation = vi.fn(() => Promise.reject(new Transient("busy")));

    const result = withRetry(operation, policy, isTransient, controller.signal);
    const settled = expect(result).rejects.toThrow("shutting down");
    await vi.advanceTimersByTimeAsync(100);
    controller.abort(new Error("shutting down"));

    await settled;
    expect(operation).toHaveBeenCalledTimes(1);
  });

  it("sleep rejects at once for a signal that is already aborted", async () => {
    await expect(sleep(1_000, AbortSignal.abort(new Error("gone")))).rejects.toThrow("gone");
  });
});
