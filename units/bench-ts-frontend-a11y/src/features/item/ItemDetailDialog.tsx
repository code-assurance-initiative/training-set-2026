import { useEffect, useId, useState } from 'react';
import type { CatalogueItem } from '../../api/types';
import { availabilityText, formatLabel } from '../../catalogue/format';
import { useCatalogueClient } from '../../context/catalogueClient';

interface LoadedItem {
  readonly itemId: string;
  readonly item: CatalogueItem | null;
}

interface ItemDetailDialogProps {
  readonly itemId: string;
  readonly onClose: () => void;
  readonly onBorrow: (item: CatalogueItem) => void;
}

export function ItemDetailDialog({ itemId, onClose, onBorrow }: ItemDetailDialogProps) {
  const client = useCatalogueClient();
  const [loaded, setLoaded] = useState<LoadedItem | null>(null);
  const titleId = useId();

  useEffect(() => {
    const controller = new AbortController();
    client.getItem(itemId, controller.signal).then(
      (found) => {
        setLoaded({ itemId, item: found });
      },
      () => {
        if (!controller.signal.aborted) {
          setLoaded({ itemId, item: null });
        }
      },
    );
    return () => {
      controller.abort();
    };
  }, [client, itemId]);

  const current = loaded?.itemId === itemId ? loaded : null;
  const item = current?.item ?? null;
  const failed = current !== null && current.item === null;

  return (
    <div className="dialog-backdrop">
      <div className="dialog" role="dialog" aria-labelledby={titleId}>
        <button type="button" className="dialog__close" aria-label="Close" onClick={onClose}>
          <svg className="icon" aria-hidden="true" focusable="false" width="20" height="20">
            <use href="/icons.svg#close" />
          </svg>
        </button>
        {failed && <p role="alert">This item could not be loaded. Please try again later.</p>}
        {item && (
          <div className="dialog__body">
            {item.coverUrl && (
              <img className="dialog__cover" src={item.coverUrl} width="120" height="180" />
            )}
            <div className="dialog__details">
              <h2 id={titleId}>{item.title}</h2>
              <p className="dialog__byline">
                {item.author} · {item.year} · {formatLabel(item.format)}
              </p>
              <div
                className="dialog__summary"
                dangerouslySetInnerHTML={{ __html: item.summaryHtml }}
              />
              <dl className="dialog__facts">
                <dt>Shelf mark</dt>
                <dd>{item.shelfMark}</dd>
                <dt>Subjects</dt>
                <dd>{item.subjects.join(', ')}</dd>
                <dt>Availability</dt>
                <dd>{availabilityText(item)}</dd>
              </dl>
              <button
                type="button"
                className="button button--primary"
                onClick={() => {
                  onBorrow(item);
                }}
              >
                Borrow this item
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
