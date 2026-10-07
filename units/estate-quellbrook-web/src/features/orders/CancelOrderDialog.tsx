import { useEffect, useId, useRef, useState, type MouseEvent, type SyntheticEvent } from 'react';
import { cancelOrder } from '../../api/orders';
import { ErrorMessage } from '../../components/ErrorMessage';

/** A native modal dialog: focus moves into it, Escape and the Close button close it, a backdrop click does too. */
export function CancelOrderDialog({
  orderId,
  onClose,
  onCancelled,
}: {
  orderId: string;
  onClose: () => void;
  onCancelled: () => void;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  const reasonId = useId();
  const [reason, setReason] = useState('');
  const [error, setError] = useState<unknown>();

  useEffect(() => {
    dialog.current?.showModal();
  }, []);

  const submit = (event: SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    cancelOrder(orderId, reason.trim()).then(onCancelled, (failure: unknown) => {
      setError(failure);
    });
  };

  const closeOnBackdrop = (event: MouseEvent<HTMLDialogElement>) => {
    if (event.target === dialog.current) {
      dialog.current.close();
    }
  };

  return (
    <dialog
      ref={dialog}
      aria-labelledby={`${reasonId}-title`}
      onClose={onClose}
      onClick={closeOnBackdrop}
    >
      <form onSubmit={submit}>
        <h2 id={`${reasonId}-title`}>Cancel this order</h2>
        <label htmlFor={reasonId}>Reason</label>
        <textarea
          id={reasonId}
          required
          maxLength={200}
          value={reason}
          onChange={(event) => {
            setReason(event.target.value);
          }}
        />
        {error !== undefined && <ErrorMessage error={error} />}
        <div className="actions">
          <button type="submit" disabled={reason.trim().length === 0}>
            Cancel order
          </button>
          <button
            type="button"
            onClick={() => {
              dialog.current?.close();
            }}
          >
            Close
          </button>
        </div>
      </form>
    </dialog>
  );
}
