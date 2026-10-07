import { useToasts } from '../context/toasts';

/** Live region for confirmations and errors; each toast stays until it is dismissed. */
export function ToastRegion() {
  const { toasts, dismiss } = useToasts();

  return (
    <div className="toast-region" role="status" aria-live="polite">
      {toasts.map((toast) => (
        <div key={toast.id} className={`toast toast--${toast.tone}`}>
          <p className="toast__message">{toast.message}</p>
          <button
            type="button"
            className="toast__dismiss"
            onClick={() => {
              dismiss(toast.id);
            }}
          >
            <svg className="icon" aria-hidden="true" focusable="false" width="16" height="16">
              <use href="/icons.svg#close" />
            </svg>
          </button>
        </div>
      ))}
    </div>
  );
}
