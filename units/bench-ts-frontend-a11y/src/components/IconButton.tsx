export type IconName = 'close' | 'previous' | 'next';

interface IconButtonProps {
  readonly icon: IconName;
  readonly label: string;
  readonly onClick: () => void;
  readonly className?: string | undefined;
}

/** A button that shows only an icon; `label` is its accessible name and its tooltip. */
export function IconButton({ icon, label, onClick, className }: IconButtonProps) {
  return (
    <button
      type="button"
      className={className ?? 'icon-button'}
      aria-label={label}
      title={label}
      onClick={onClick}
    >
      <svg className="icon" aria-hidden="true" focusable="false" width="20" height="20">
        <use href={`/icons.svg#${icon}`} />
      </svg>
    </button>
  );
}
