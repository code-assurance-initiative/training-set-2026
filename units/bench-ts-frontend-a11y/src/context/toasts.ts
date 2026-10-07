import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';

export type ToastTone = 'success' | 'error' | 'info';

export interface Toast {
  readonly id: number;
  readonly tone: ToastTone;
  readonly message: string;
}

export interface ToastApi {
  readonly toasts: readonly Toast[];
  readonly notify: (tone: ToastTone, message: string) => void;
  readonly dismiss: (id: number) => void;
}

export const ToastContext = createContext<ToastApi | null>(null);

/** Owns the toast queue; the newest toast is shown last and at most `limit` are kept. */
export function useToastState(limit = 3): ToastApi {
  const [toasts, setToasts] = useState<readonly Toast[]>([]);
  const nextId = useRef(1);

  const notify = useCallback(
    (tone: ToastTone, message: string) => {
      const id = nextId.current;
      nextId.current += 1;
      setToasts((current) => [...current, { id, tone, message }].slice(-limit));
    },
    [limit],
  );

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  return useMemo(() => ({ toasts, notify, dismiss }), [toasts, notify, dismiss]);
}

export function useToasts(): ToastApi {
  const api = useContext(ToastContext);
  if (!api) {
    throw new Error('useToasts must be used inside a ToastContext provider');
  }
  return api;
}
