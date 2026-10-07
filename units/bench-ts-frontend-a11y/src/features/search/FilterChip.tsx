import type { KeyboardEvent } from 'react';

interface FilterChipProps {
  readonly label: string;
  readonly checked: boolean;
  readonly onToggle: () => void;
}

/** A toggleable chip that behaves as a checkbox for keyboard and assistive-technology users. */
export function FilterChip({ label, checked, onToggle }: FilterChipProps) {
  const handleKeyDown = (event: KeyboardEvent<HTMLSpanElement>) => {
    if (event.key === ' ' || event.key === 'Enter') {
      event.preventDefault();
      onToggle();
    }
  };

  return (
    <span
      role="checkbox"
      aria-checked={checked}
      tabIndex={0}
      className={checked ? 'chip chip--checked' : 'chip'}
      onClick={onToggle}
      onKeyDown={handleKeyDown}
    >
      {label}
    </span>
  );
}
