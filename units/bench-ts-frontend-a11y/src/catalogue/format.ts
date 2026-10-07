import type { CatalogueItem, ItemFormat } from '../api/types';

const FORMAT_LABELS: Readonly<Record<ItemFormat, string>> = {
  book: 'Book',
  audiobook: 'Audiobook',
  dvd: 'DVD',
  magazine: 'Magazine',
};

export function formatLabel(format: ItemFormat): string {
  return FORMAT_LABELS[format];
}

export function availabilityText(
  item: Pick<CatalogueItem, 'availableCopies' | 'totalCopies'>,
): string {
  if (item.availableCopies === 0) {
    return `All ${item.totalCopies} copies on loan`;
  }
  return `${item.availableCopies} of ${item.totalCopies} available`;
}

export function formatDate(isoDate: string, locale = 'en-GB'): string {
  return new Date(`${isoDate}T00:00:00Z`).toLocaleDateString(locale, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  });
}
