import { useId, type ReactNode } from 'react';

interface CheckboxFieldProps {
  readonly label: ReactNode;
  readonly checked: boolean;
  readonly onChange: (checked: boolean) => void;
}

/** A checkbox with its label beside it, the way every form of the app lays one out. */
export function CheckboxField({ label, checked, onChange }: CheckboxFieldProps) {
  const id = useId();
  return (
    <div className="form-field form-field--inline">
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={(event) => {
          onChange(event.target.checked);
        }}
      />
      <label htmlFor={id}>{label}</label>
    </div>
  );
}
