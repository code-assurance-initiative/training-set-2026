import { ApiError, type CatalogueClient } from './catalogueClient';
import type { CatalogueItem, LibraryNotice, Loan, SearchQuery } from './types';

export const PAGE_SIZE = 5;

const ITEMS: readonly CatalogueItem[] = [
  {
    id: 'b-1001',
    title: 'The Tide Clock',
    author: 'Maren Holt',
    year: 2021,
    format: 'book',
    shelfMark: 'FIC HOL',
    subjects: ['Coastal towns', 'Family sagas'],
    coverUrl: '/images/covers/tide-clock.jpg',
    summaryHtml:
      '<p>Three generations of a harbour family and the clock that keeps their tides.</p>',
    availableCopies: 2,
    totalCopies: 3,
  },
  {
    id: 'b-1002',
    title: 'Growing Food in Small Spaces',
    author: 'Idris Okafor',
    year: 2019,
    format: 'book',
    shelfMark: '635 OKA',
    subjects: ['Gardening', 'Urban living'],
    coverUrl: '/images/covers/small-spaces.jpg',
    summaryHtml: '<p>Balcony, windowsill and allotment growing, season by season.</p>',
    availableCopies: 0,
    totalCopies: 2,
  },
  {
    id: 'a-2001',
    title: 'The Tide Clock (audiobook)',
    author: 'Maren Holt',
    year: 2022,
    format: 'audiobook',
    shelfMark: 'AUD HOL',
    subjects: ['Coastal towns', 'Family sagas'],
    coverUrl: '/images/covers/tide-clock-audio.jpg',
    summaryHtml: '<p>Unabridged, read by the author.</p>',
    availableCopies: 1,
    totalCopies: 1,
  },
  {
    id: 'd-3001',
    title: 'Birds of the Estuary',
    author: 'Northshore Wildlife Trust',
    year: 2018,
    format: 'dvd',
    shelfMark: 'DVD 598 BIR',
    subjects: ['Birds', 'Wetlands'],
    coverUrl: null,
    summaryHtml: '<p>A year on the mudflats, filmed over four seasons.</p>',
    availableCopies: 1,
    totalCopies: 1,
  },
  {
    id: 'm-4001',
    title: 'Local History Quarterly, Spring issue',
    author: 'Harbour Historical Society',
    year: 2026,
    format: 'magazine',
    shelfMark: 'PER LOC',
    subjects: ['Local history'],
    coverUrl: '/images/covers/local-history.jpg',
    summaryHtml: '<p>The old ferry crossing, the mill fire of 1911, and a map of lost lanes.</p>',
    availableCopies: 3,
    totalCopies: 3,
  },
  {
    id: 'b-1003',
    title: 'First Steps in Python',
    author: 'Ana Lindqvist',
    year: 2024,
    format: 'book',
    shelfMark: '005.133 LIN',
    subjects: ['Programming'],
    coverUrl: '/images/covers/first-steps-python.jpg',
    summaryHtml: '<p>A gentle introduction for adult learners, with exercises.</p>',
    availableCopies: 4,
    totalCopies: 4,
  },
  {
    id: 'b-1004',
    title: 'Knots and Splices',
    author: 'Tomas Reyes',
    year: 2015,
    format: 'book',
    shelfMark: '623.88 REY',
    subjects: ['Sailing', 'Crafts'],
    coverUrl: '/images/covers/knots.jpg',
    summaryHtml: '<p>Forty knots every sailor should know, illustrated step by step.</p>',
    availableCopies: 1,
    totalCopies: 2,
  },
];

const NOTICE: LibraryNotice = {
  id: 'n-17',
  title: 'Hillside Branch closed on Monday',
  bodyHtml:
    '<p>Hillside Branch is closed on <strong>Monday 12 October</strong> for floor repairs. Returns can be left at Central Library.</p>',
};

function matches(item: CatalogueItem, query: SearchQuery): boolean {
  const text = query.text.trim().toLowerCase();
  const textMatches =
    text.length === 0 ||
    item.title.toLowerCase().includes(text) ||
    item.author.toLowerCase().includes(text) ||
    item.subjects.some((subject) => subject.toLowerCase().includes(text));
  const formatMatches = query.formats.length === 0 || query.formats.includes(item.format);
  return textMatches && formatMatches && (!query.availableOnly || item.availableCopies > 0);
}

function addDays(date: Date, days: number): string {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy.toISOString().slice(0, 10);
}

/** An in-memory catalogue for local development and demos, shaped exactly like the HTTP API. */
export function createDemoClient(today: Date = new Date()): CatalogueClient {
  let loans: Loan[] = [
    {
      loanId: 'l-1',
      itemId: 'b-1004',
      title: 'Knots and Splices',
      author: 'Tomas Reyes',
      borrowedOn: addDays(today, -20),
      dueOn: addDays(today, 1),
      renewals: 0,
      maxRenewals: 2,
      fineCents: 0,
    },
    {
      loanId: 'l-2',
      itemId: 'b-1002',
      title: 'Growing Food in Small Spaces',
      author: 'Idris Okafor',
      borrowedOn: addDays(today, -40),
      dueOn: addDays(today, -5),
      renewals: 2,
      maxRenewals: 2,
      fineCents: 250,
    },
  ];
  let reservations = 0;

  return {
    search(query) {
      const found = ITEMS.filter((item) => matches(item, query));
      const start = (query.page - 1) * PAGE_SIZE;
      return Promise.resolve({
        items: found.slice(start, start + PAGE_SIZE),
        total: found.length,
        page: query.page,
        pageSize: PAGE_SIZE,
      });
    },
    getItem(id) {
      const item = ITEMS.find((candidate) => candidate.id === id);
      return item ? Promise.resolve(item) : Promise.reject(new ApiError(404, `No item ${id}`));
    },
    borrow(request) {
      reservations += 1;
      return Promise.resolve({
        reservationId: `r-${reservations}`,
        itemId: request.itemId,
        pickupBranch: request.pickupBranch,
        readyBy: addDays(today, 2),
      });
    },
    listLoans() {
      return Promise.resolve(loans);
    },
    renew(loanId) {
      const loan = loans.find((candidate) => candidate.loanId === loanId);
      if (!loan || loan.renewals >= loan.maxRenewals) {
        return Promise.reject(new ApiError(409, `Loan ${loanId} cannot be renewed`));
      }
      const renewed: Loan = { ...loan, renewals: loan.renewals + 1, dueOn: addDays(today, 21) };
      loans = loans.map((candidate) => (candidate.loanId === loanId ? renewed : candidate));
      return Promise.resolve(renewed);
    },
    currentNotice() {
      return Promise.resolve(NOTICE);
    },
  };
}
