import { useId, type ReactNode } from 'react';

interface TextFieldProps {
  readonly label: ReactNode;
  readonly value: string;
  readonly onChange: (value: string) => void;
}

/** A one-line text input under its label. */
export function TextField({ label, value, onChange }: TextFieldProps) {
  const id = useId();
  return (
    <div className="form-field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        value={value}
        onChange={(event) => {
          onChange(event.target.value);
        }}
      />
    </div>
  );
}
