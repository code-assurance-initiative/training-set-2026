import { useEffect, useEffectEvent, useState } from 'react';

export type Load<T> =
  | { readonly state: 'loading' }
  | { readonly state: 'loaded'; readonly value: T }
  | { readonly state: 'failed'; readonly error: unknown };

/**
 * Runs a request whenever `key` changes (or `reload` is called) and cancels the previous one. The result is kept
 * per key, so a new key shows "loading" until its own answer arrives.
 */
export function useLoad<T>(
  load: (signal: AbortSignal) => Promise<T>,
  key: string,
): [Load<T>, () => void] {
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState<{ key: string; attempt: number; load: Load<T> }>();
  const run = useEffectEvent(load);
  useEffect(() => {
    const controller = new AbortController();
    run(controller.signal).then(
      (value) => {
        setResult({ key, attempt, load: { state: 'loaded', value } });
      },
      (error: unknown) => {
        if (!controller.signal.aborted) {
          setResult({ key, attempt, load: { state: 'failed', error } });
        }
      },
    );
    return () => {
      controller.abort();
    };
  }, [key, attempt]);
  const current: Load<T> =
    result?.key === key && result.attempt === attempt ? result.load : { state: 'loading' };
  return [
    current,
    () => {
      setAttempt((value) => value + 1);
    },
  ];
}
