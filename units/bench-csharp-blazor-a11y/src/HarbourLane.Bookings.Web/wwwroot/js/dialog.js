// Native <dialog> helpers. showModal() moves focus into the dialog, makes the rest of the page inert,
// closes on Escape and returns focus to the element that had it when the dialog closes.

export function showDialogModal(dialog) {
    if (dialog && !dialog.open) {
        dialog.showModal();
    }
}

export function closeDialog(dialog) {
    if (dialog && dialog.open) {
        dialog.close();
    }
}
