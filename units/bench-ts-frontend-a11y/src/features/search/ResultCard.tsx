import type { MouseEvent } from 'react';
import type { CatalogueItem } from '../../api/types';
import { availabilityText, formatLabel } from '../../catalogue/format';

interface ResultCardProps {
  readonly item: CatalogueItem;
  readonly onOpen: (itemId: string) => void;
}

export function ResultCard({ item, onOpen }: ResultCardProps) {
  const open = () => {
    onOpen(item.id);
  };

  // The whole card is a pointer target; clicks on the card's own button are handled by the button.
  const handleCardClick = (event: MouseEvent<HTMLElement>) => {
    if (event.target instanceof Element && event.target.closest('button')) {
      return;
    }
    open();
  };

  return (
    <article className="result-card" onClick={handleCardClick}>
      <h3 className="result-card__title">{item.title}</h3>
      <p className="result-meta">
        {item.author} · {item.year} · {formatLabel(item.format)} · {item.shelfMark}
      </p>
      <p className="result-card__availability">{availabilityText(item)}</p>
      <button type="button" className="button button--secondary" onClick={open}>
        View details<span className="visually-hidden"> of {item.title}</span>
      </button>
    </article>
  );
}
