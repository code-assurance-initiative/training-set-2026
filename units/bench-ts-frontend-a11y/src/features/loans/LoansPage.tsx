import { useCallback, useEffect, useId, useMemo, useState } from 'react';
import { ApiError } from '../../api/catalogueClient';
import type { Loan } from '../../api/types';
import { formatDate } from '../../catalogue/format';
import { useCatalogueClient } from '../../context/catalogueClient';
import { usePreferences } from '../../context/settings';
import { useToasts } from '../../context/toasts';

// ---------------------------------------------------------------------------------------------
// Dates, money and loan status
// ---------------------------------------------------------------------------------------------

const MS_PER_DAY = 24 * 60 * 60 * 1000;

type DueStatus = 'overdue' | 'due-soon' | 'on-loan';

function startOfDayUtc(date: Date): number {
  return Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
}

function daysUntil(isoDate: string, today: Date): number {
  const due = Date.parse(`${isoDate}T00:00:00Z`);
  return Math.round((due - startOfDayUtc(today)) / MS_PER_DAY);
}

function dueStatus(loan: Loan, today: Date, reminderDays: number): DueStatus {
  const days = daysUntil(loan.dueOn, today);
  if (days < 0) {
    return 'overdue';
  }
  return days <= reminderDays ? 'due-soon' : 'on-loan';
}

function canRenew(loan: Loan, today: Date): boolean {
  return loan.renewals < loan.maxRenewals && daysUntil(loan.dueOn, today) >= 0;
}

function formatMoney(cents: number): string {
  return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' }).format(cents / 100);
}

function dueText(loan: Loan, today: Date): string {
  const days = daysUntil(loan.dueOn, today);
  if (days < 0) {
    return days === -1 ? 'Overdue by 1 day' : `Overdue by ${-days} days`;
  }
  if (days === 0) {
    return 'Due today';
  }
  return days === 1 ? 'Due tomorrow' : `Due in ${days} days`;
}

// ---------------------------------------------------------------------------------------------
// Sorting and filtering
// ---------------------------------------------------------------------------------------------

type SortKey = 'title' | 'author' | 'borrowedOn' | 'dueOn';
type SortDirection = 'ascending' | 'descending';

interface SortOrder {
  readonly key: SortKey;
  readonly direction: SortDirection;
}

const SORTABLE_COLUMNS: readonly { readonly key: SortKey; readonly label: string }[] = [
  { key: 'title', label: 'Title' },
  { key: 'author', label: 'Author' },
  { key: 'borrowedOn', label: 'Borrowed' },
  { key: 'dueOn', label: 'Due' },
];

function sortLoans(loans: readonly Loan[], order: SortOrder): Loan[] {
  const factor = order.direction === 'ascending' ? 1 : -1;
  return [...loans].sort((a, b) => factor * a[order.key].localeCompare(b[order.key], 'en-GB'));
}

function filterLoans(loans: readonly Loan[], text: string): readonly Loan[] {
  const needle = text.trim().toLowerCase();
  if (needle.length === 0) {
    return loans;
  }
  return loans.filter(
    (loan) =>
      loan.title.toLowerCase().includes(needle) || loan.author.toLowerCase().includes(needle),
  );
}

function nextOrder(current: SortOrder, key: SortKey): SortOrder {
  if (current.key !== key) {
    return { key, direction: 'ascending' };
  }
  return { key, direction: current.direction === 'ascending' ? 'descending' : 'ascending' };
}

// ---------------------------------------------------------------------------------------------
// CSV export
// ---------------------------------------------------------------------------------------------

const CSV_COLUMNS: readonly { readonly header: string; readonly value: (loan: Loan) => string }[] =
  [
    { header: 'Title', value: (loan) => loan.title },
    { header: 'Author', value: (loan) => loan.author },
    { header: 'Borrowed', value: (loan) => loan.borrowedOn },
    { header: 'Due', value: (loan) => loan.dueOn },
    { header: 'Renewals', value: (loan) => `${loan.renewals}/${loan.maxRenewals}` },
    { header: 'Fine', value: (loan) => (loan.fineCents / 100).toFixed(2) },
  ];

function escapeCsvField(value: string): string {
  // Leading formula characters are neutralised so a spreadsheet never evaluates a title.
  const neutralised = /^[=+\-@\t\r]/.test(value) ? `'${value}` : value;
  return /[",\n\r]/.test(neutralised) ? `"${neutralised.replace(/"/g, '""')}"` : neutralised;
}

function loansToCsv(loans: readonly Loan[]): string {
  const header = CSV_COLUMNS.map((column) => column.header).join(',');
  const rows = loans.map((loan) =>
    CSV_COLUMNS.map((column) => escapeCsvField(column.value(loan))).join(','),
  );
  return [header, ...rows].join('\r\n');
}

function downloadCsv(fileName: string, csv: string): void {
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

// ---------------------------------------------------------------------------------------------
// Data: loading and renewing
// ---------------------------------------------------------------------------------------------

interface RenewalOutcome {
  readonly loanId: string;
  readonly title: string;
  readonly renewed: boolean;
  readonly detail: string;
}

function renewalFailure(loan: Loan, reason: unknown): RenewalOutcome {
  const detail =
    reason instanceof ApiError && reason.status === 409
      ? 'No renewals left or someone has reserved it'
      : 'The library system did not answer';
  return { loanId: loan.loanId, title: loan.title, renewed: false, detail };
}

function useLoans() {
  const client = useCatalogueClient();
  const [loans, setLoans] = useState<readonly Loan[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadFailed, setLoadFailed] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    client.listLoans(controller.signal).then(
      (loaded) => {
        setLoans(loaded);
        setLoading(false);
      },
      () => {
        if (!controller.signal.aborted) {
          setLoadFailed(true);
          setLoading(false);
        }
      },
    );
    return () => {
      controller.abort();
    };
  }, [client]);

  const renew = useCallback(
    async (loan: Loan): Promise<RenewalOutcome> => {
      try {
        const renewed = await client.renew(loan.loanId);
        setLoans((current) =>
          current.map((candidate) => (candidate.loanId === renewed.loanId ? renewed : candidate)),
        );
        return {
          loanId: loan.loanId,
          title: loan.title,
          renewed: true,
          detail: `Now due ${formatDate(renewed.dueOn)}`,
        };
      } catch (reason) {
        return renewalFailure(loan, reason);
      }
    },
    [client],
  );

  return { loans, loading, loadFailed, renew };
}

// ---------------------------------------------------------------------------------------------
// Presentation
// ---------------------------------------------------------------------------------------------

interface LoansToolbarProps {
  readonly filterText: string;
  readonly onFilterChange: (text: string) => void;
  readonly renewableCount: number;
  readonly renewing: boolean;
  readonly onRenewAll: () => void;
  readonly onExport: () => void;
}

function LoansToolbar({
  filterText,
  onFilterChange,
  renewableCount,
  renewing,
  onRenewAll,
  onExport,
}: LoansToolbarProps) {
  return (
    <div className="loans-toolbar">
      <input
        className="loans-toolbar__filter"
        type="search"
        placeholder="Filter by title or author"
        value={filterText}
        onChange={(event) => {
          onFilterChange(event.target.value);
        }}
      />
      <button
        type="button"
        className="button button--primary"
        disabled={renewableCount === 0 || renewing}
        onClick={onRenewAll}
      >
        Renew all eligible loans ({renewableCount})
      </button>
      <button type="button" className="button button--secondary" onClick={onExport}>
        Export loans as CSV
      </button>
    </div>
  );
}

interface SortableHeaderProps {
  readonly column: SortKey;
  readonly label: string;
  readonly order: SortOrder;
  readonly onSort: (key: SortKey) => void;
}

function SortableHeader({ column, label, order, onSort }: SortableHeaderProps) {
  const active = order.key === column;
  return (
    <th scope="col" aria-sort={active ? order.direction : 'none'}>
      <button
        type="button"
        className="table-sort"
        onClick={() => {
          onSort(column);
        }}
      >
        {label}
        <span className="table-sort__indicator" aria-hidden="true">
          {active ? (order.direction === 'ascending' ? '▲' : '▼') : '↕'}
        </span>
      </button>
    </th>
  );
}

interface DueBadgeProps {
  readonly status: DueStatus;
  readonly text: string;
}

function DueBadge({ status, text }: DueBadgeProps) {
  return <span className={`due-badge due-badge--${status}`}>{text}</span>;
}

interface LoanRowProps {
  readonly loan: Loan;
  readonly today: Date;
  readonly reminderDays: number;
  readonly renewing: boolean;
  readonly onRenew: (loan: Loan) => void;
}

function LoanRow({ loan, today, reminderDays, renewing, onRenew }: LoanRowProps) {
  const renewable = canRenew(loan, today);
  return (
    <tr className={`loan-row loan-row--${dueStatus(loan, today, reminderDays)}`}>
      <th scope="row">{loan.title}</th>
      <td>{loan.author}</td>
      <td>{formatDate(loan.borrowedOn)}</td>
      <td>
        {formatDate(loan.dueOn)}{' '}
        <DueBadge status={dueStatus(loan, today, reminderDays)} text={dueText(loan, today)} />
      </td>
      <td>
        {loan.renewals} of {loan.maxRenewals}
      </td>
      <td>
        {renewable ? (
          <button
            type="button"
            className="button button--small"
            disabled={renewing}
            onClick={() => {
              onRenew(loan);
            }}
          >
            Renew<span className="visually-hidden"> {loan.title}</span>
          </button>
        ) : (
          <span className="loan-row__note">Cannot be renewed</span>
        )}
      </td>
    </tr>
  );
}

interface LoansTableProps {
  readonly loans: readonly Loan[];
  readonly order: SortOrder;
  readonly onSort: (key: SortKey) => void;
  readonly today: Date;
  readonly reminderDays: number;
  readonly renewing: boolean;
  readonly onRenew: (loan: Loan) => void;
}

function LoansTable({
  loans,
  order,
  onSort,
  today,
  reminderDays,
  renewing,
  onRenew,
}: LoansTableProps) {
  return (
    <table className="loans-table">
      <caption>Items you have on loan</caption>
      <thead>
        <tr>
          {SORTABLE_COLUMNS.map((column) => (
            <SortableHeader
              key={column.key}
              column={column.key}
              label={column.label}
              order={order}
              onSort={onSort}
            />
          ))}
          <th scope="col">Renewals</th>
          <th scope="col">Actions</th>
        </tr>
      </thead>
      <tbody>
        {loans.map((loan) => (
          <LoanRow
            key={loan.loanId}
            loan={loan}
            today={today}
            reminderDays={reminderDays}
            renewing={renewing}
            onRenew={onRenew}
          />
        ))}
      </tbody>
    </table>
  );
}

interface RenewalResultsProps {
  readonly outcomes: readonly RenewalOutcome[];
}

function RenewalResults({ outcomes }: RenewalResultsProps) {
  const renewed = outcomes.filter((outcome) => outcome.renewed).length;
  return (
    <div className="renewal-results" role="status">
      <p className="renewal-results__summary">
        {renewed} of {outcomes.length} {outcomes.length === 1 ? 'loan' : 'loans'} renewed.
      </p>
      <ul className="renewal-results__list">
        {outcomes.map((outcome) => (
          <li key={outcome.loanId} className={outcome.renewed ? 'renewed' : 'not-renewed'}>
            <strong>{outcome.title}</strong>: {outcome.detail}
          </li>
        ))}
      </ul>
    </div>
  );
}

interface FinesSummaryProps {
  readonly loans: readonly Loan[];
}

function FinesSummary({ loans }: FinesSummaryProps) {
  const withFines = loans.filter((loan) => loan.fineCents > 0);
  const totalCents = withFines.reduce((sum, loan) => sum + loan.fineCents, 0);
  if (withFines.length === 0) {
    return <p className="fines fines--none">You have no fines.</p>;
  }
  return (
    <section className="fines" aria-labelledby="fines-title">
      <h2 id="fines-title">Fines</h2>
      <dl className="fines__list">
        {withFines.map((loan) => (
          <div key={loan.loanId} className="fines__entry">
            <dt>{loan.title}</dt>
            <dd>{formatMoney(loan.fineCents)}</dd>
          </div>
        ))}
        <div className="fines__entry fines__entry--total">
          <dt>Total</dt>
          <dd>{formatMoney(totalCents)}</dd>
        </div>
      </dl>
      <p>Fines can be paid at any branch desk, by card or in cash.</p>
    </section>
  );
}

interface NextDueProps {
  readonly loans: readonly Loan[];
  readonly today: Date;
}

function NextDue({ loans, today }: NextDueProps) {
  const upcoming = loans
    .filter((loan) => daysUntil(loan.dueOn, today) >= 0)
    .sort((a, b) => a.dueOn.localeCompare(b.dueOn));
  const next = upcoming[0];
  if (!next) {
    return null;
  }
  return (
    <p className="next-due">
      Next due: <strong>{next.title}</strong>, {dueText(next, today).toLowerCase()} (
      {formatDate(next.dueOn)}).
    </p>
  );
}

// ---------------------------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------------------------

interface LoansPageProps {
  readonly today?: Date;
}

export function LoansPage({ today = new Date() }: LoansPageProps) {
  const { loans, loading, loadFailed, renew } = useLoans();
  const { preferences } = usePreferences();
  const { notify } = useToasts();
  const [filterText, setFilterText] = useState('');
  const [order, setOrder] = useState<SortOrder>({ key: 'dueOn', direction: 'ascending' });
  const [renewing, setRenewing] = useState(false);
  const [outcomes, setOutcomes] = useState<readonly RenewalOutcome[]>([]);
  const headingId = useId();

  const visible = useMemo(
    () => sortLoans(filterLoans(loans, filterText), order),
    [loans, filterText, order],
  );
  const renewable = useMemo(() => loans.filter((loan) => canRenew(loan, today)), [loans, today]);

  const renewOne = async (loan: Loan) => {
    setRenewing(true);
    const outcome = await renew(loan);
    setRenewing(false);
    setOutcomes([outcome]);
    notify(outcome.renewed ? 'success' : 'error', `${outcome.title}: ${outcome.detail}`);
  };

  const renewAll = async () => {
    setRenewing(true);
    const results: RenewalOutcome[] = [];
    for (const loan of renewable) {
      results.push(await renew(loan));
    }
    setRenewing(false);
    setOutcomes(results);
    const renewedCount = results.filter((result) => result.renewed).length;
    notify(renewedCount === results.length ? 'success' : 'error', `${renewedCount} loans renewed`);
  };

  const exportCsv = () => {
    downloadCsv(`loans-${today.toISOString().slice(0, 10)}.csv`, loansToCsv(visible));
  };

  if (loading) {
    return (
      <section aria-labelledby={headingId} aria-busy="true">
        <h1 id={headingId}>My loans</h1>
        <p>Loading your loans…</p>
      </section>
    );
  }

  if (loadFailed) {
    return (
      <section aria-labelledby={headingId}>
        <h1 id={headingId}>My loans</h1>
        <p role="alert">Your loans could not be loaded. Please sign in again or try later.</p>
      </section>
    );
  }

  return (
    <section className="loans" aria-labelledby={headingId}>
      <h1 id={headingId}>My loans</h1>
      {loans.length === 0 ? (
        <p>You have nothing on loan. Items you borrow appear here.</p>
      ) : (
        <>
          <NextDue loans={loans} today={today} />
          <LoansToolbar
            filterText={filterText}
            onFilterChange={setFilterText}
            renewableCount={renewable.length}
            renewing={renewing}
            onRenewAll={() => void renewAll()}
            onExport={exportCsv}
          />
          {outcomes.length > 0 && <RenewalResults outcomes={outcomes} />}
          <LoansTable
            loans={visible}
            order={order}
            onSort={(key) => {
              setOrder((current) => nextOrder(current, key));
            }}
            today={today}
            reminderDays={preferences.reminderDays}
            renewing={renewing}
            onRenew={(loan) => void renewOne(loan)}
          />
          {visible.length === 0 && <p>No loans match “{filterText}”.</p>}
        </>
      )}
      <FinesSummary loans={loans} />
    </section>
  );
}
