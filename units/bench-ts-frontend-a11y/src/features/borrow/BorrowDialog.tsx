import { useEffect, useId, useRef, type MouseEvent } from 'react';
import type { CatalogueItem, Reservation } from '../../api/types';
import { BorrowForm } from './BorrowForm';

interface BorrowDialogProps {
  readonly item: CatalogueItem;
  readonly onClose: () => void;
  readonly onReserved: (reservation: Reservation) => void;
}

export function BorrowDialog({ item, onClose, onReserved }: BorrowDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  // Opened as a modal once mounted; removing the element on unmount ends the modal state.
  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) {
      dialog.showModal();
    }
  }, []);

  const close = () => {
    dialogRef.current?.close();
  };

  // A click whose target is the <dialog> itself landed on the backdrop, outside the form.
  const closeOnBackdropClick = (event: MouseEvent<HTMLDialogElement>) => {
    if (event.target === event.currentTarget) {
      close();
    }
  };

  return (
    <dialog
      ref={dialogRef}
      className="borrow-dialog"
      aria-labelledby={titleId}
      onClose={onClose}
      onClick={closeOnBackdropClick}
    >
      <h2 id={titleId}>Borrow “{item.title}”</h2>
      <BorrowForm
        item={item}
        onCancel={close}
        onReserved={(reservation) => {
          onReserved(reservation);
          close();
        }}
      />
    </dialog>
  );
}
