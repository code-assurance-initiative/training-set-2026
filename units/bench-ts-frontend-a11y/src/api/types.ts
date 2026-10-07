export type ItemFormat = 'book' | 'audiobook' | 'dvd' | 'magazine';

export const ITEM_FORMATS: readonly ItemFormat[] = ['book', 'audiobook', 'dvd', 'magazine'];

export interface CatalogueItem {
  readonly id: string;
  readonly title: string;
  readonly author: string;
  readonly year: number;
  readonly format: ItemFormat;
  readonly shelfMark: string;
  readonly subjects: readonly string[];
  readonly coverUrl: string | null;
  /** Record summary as delivered by the catalogue API (HTML from the partner record import). */
  readonly summaryHtml: string;
  readonly availableCopies: number;
  readonly totalCopies: number;
}

export interface SearchQuery {
  readonly text: string;
  readonly formats: readonly ItemFormat[];
  readonly availableOnly: boolean;
  readonly page: number;
}

export interface SearchPage {
  readonly items: readonly CatalogueItem[];
  readonly total: number;
  readonly page: number;
  readonly pageSize: number;
}

export interface BorrowRequest {
  readonly itemId: string;
  readonly cardNumber: string;
  readonly pickupBranch: Branch;
  readonly notifyByEmail: boolean;
}

export type Branch = 'central' | 'harbour' | 'hillside';

export const BRANCHES: Readonly<Record<Branch, string>> = {
  central: 'Central Library',
  harbour: 'Harbour Branch',
  hillside: 'Hillside Branch',
};

export interface Reservation {
  readonly reservationId: string;
  readonly itemId: string;
  readonly pickupBranch: Branch;
  readonly readyBy: string;
}

export interface Loan {
  readonly loanId: string;
  readonly itemId: string;
  readonly title: string;
  readonly author: string;
  readonly borrowedOn: string;
  readonly dueOn: string;
  readonly renewals: number;
  readonly maxRenewals: number;
  readonly fineCents: number;
}

export interface LibraryNotice {
  readonly id: string;
  readonly title: string;
  /** Staff-authored rich text from the content management system. */
  readonly bodyHtml: string;
}
