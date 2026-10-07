import { describe, expect, it } from 'vitest';
import { normaliseCardNumber, validateBorrowForm } from '../src/features/borrow/borrowValidation';

const valid = {
  cardNumber: '1234 5678 9012 34',
  pickupBranch: 'central',
  notifyByEmail: true,
} as const;

describe('validateBorrowForm', () => {
  it('accepts a 14-digit card number with or without spaces', () => {
    expect(validateBorrowForm(valid)).toEqual({});
    expect(validateBorrowForm({ ...valid, cardNumber: '12345678901234' })).toEqual({});
  });

  it('asks for a missing card number', () => {
    expect(validateBorrowForm({ ...valid, cardNumber: '  ' }).cardNumber).toBe(
      'Enter your library card number.',
    );
  });

  it('rejects a card number of the wrong length', () => {
    expect(validateBorrowForm({ ...valid, cardNumber: '1234 5678' }).cardNumber).toBe(
      'A library card number has 14 digits.',
    );
  });
});

describe('normaliseCardNumber', () => {
  it('removes the spaces printed between digit groups', () => {
    expect(normaliseCardNumber('1234 5678 9012 34')).toBe('12345678901234');
  });
});
