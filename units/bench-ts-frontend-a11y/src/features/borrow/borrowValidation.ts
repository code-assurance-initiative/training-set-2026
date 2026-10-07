import type { Branch } from '../../api/types';

export interface BorrowFormValues {
  readonly cardNumber: string;
  readonly pickupBranch: Branch;
  readonly notifyByEmail: boolean;
}

export type BorrowFormErrors = Partial<Record<keyof BorrowFormValues, string>>;

const CARD_NUMBER = /^\d{4} ?\d{4} ?\d{4} ?\d{2}$/;

/** Library cards carry a 14-digit number, printed in groups of four (spaces optional). */
export function normaliseCardNumber(input: string): string {
  return input.replace(/\s+/g, '');
}

export function validateBorrowForm(values: BorrowFormValues): BorrowFormErrors {
  const errors: BorrowFormErrors = {};
  const card = values.cardNumber.trim();
  if (card.length === 0) {
    errors.cardNumber = 'Enter your library card number.';
  } else if (!CARD_NUMBER.test(card)) {
    errors.cardNumber = 'A library card number has 14 digits.';
  }
  return errors;
}
