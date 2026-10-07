import { useId, useState, type SubmitEvent } from 'react';
import { BRANCHES, type Branch, type CatalogueItem, type Reservation } from '../../api/types';
import { CheckboxField } from '../../components/CheckboxField';
import { useCatalogueClient } from '../../context/catalogueClient';
import { usePreferences } from '../../context/settings';
import { normaliseCardNumber, validateBorrowForm, type BorrowFormErrors } from './borrowValidation';

interface BorrowFormProps {
  readonly item: CatalogueItem;
  readonly onReserved: (reservation: Reservation) => void;
  readonly onCancel: () => void;
}

/** Card number, pickup branch and notification choice for one reservation. */
export function BorrowForm({ item, onReserved, onCancel }: BorrowFormProps) {
  const client = useCatalogueClient();
  const { preferences } = usePreferences();
  const [cardNumber, setCardNumber] = useState('');
  const [pickupBranch, setPickupBranch] = useState<Branch>(preferences.homeBranch);
  const [notifyByEmail, setNotifyByEmail] = useState(preferences.emailReminders);
  const [errors, setErrors] = useState<BorrowFormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const cardId = useId();
  const cardHintId = useId();
  const cardErrorId = useId();
  const branchId = useId();

  const handleSubmit = async (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    const found = validateBorrowForm({ cardNumber, pickupBranch, notifyByEmail });
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }
    setSubmitting(true);
    try {
      const reservation = await client.borrow({
        itemId: item.id,
        cardNumber: normaliseCardNumber(cardNumber),
        pickupBranch,
        notifyByEmail,
      });
      onReserved(reservation);
    } catch {
      setErrors({ cardNumber: 'The reservation could not be made. Check the card number.' });
    } finally {
      setSubmitting(false);
    }
  };

  const cardDescribedBy = errors.cardNumber ? `${cardHintId} ${cardErrorId}` : cardHintId;

  return (
    <form className="borrow-form" noValidate onSubmit={(event) => void handleSubmit(event)}>
      <div className="form-field">
        <label htmlFor={cardId}>Library card number</label>
        <p id={cardHintId} className="form-field__hint">
          The 14 digits under the barcode on your card.
        </p>
        <input
          id={cardId}
          inputMode="numeric"
          autoComplete="off"
          value={cardNumber}
          aria-invalid={errors.cardNumber ? true : undefined}
          aria-describedby={cardDescribedBy}
          onChange={(event) => {
            setCardNumber(event.target.value);
          }}
        />
        {errors.cardNumber && (
          <p id={cardErrorId} className="form-field__error">
            {errors.cardNumber}
          </p>
        )}
      </div>
      <div className="form-field">
        <label htmlFor={branchId}>Pick up at</label>
        <select
          id={branchId}
          value={pickupBranch}
          onChange={(event) => {
            setPickupBranch(event.target.value as Branch);
          }}
        >
          {Object.entries(BRANCHES).map(([value, name]) => (
            <option key={value} value={value}>
              {name}
            </option>
          ))}
        </select>
      </div>
      <CheckboxField
        label="Email me when it is ready to collect"
        checked={notifyByEmail}
        onChange={setNotifyByEmail}
      />
      <div className="borrow-form__actions">
        <button type="submit" className="button button--primary" disabled={submitting}>
          Reserve for pickup
        </button>
        <button type="button" className="button button--secondary" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}
