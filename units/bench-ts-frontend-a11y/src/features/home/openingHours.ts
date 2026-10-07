export const WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'] as const;

export interface BranchHours {
  readonly name: string;
  /** One entry per weekday, Monday first; `null` when the branch is closed that day. */
  readonly hours: readonly (string | null)[];
}

export const OPENING_HOURS: readonly BranchHours[] = [
  {
    name: 'Central Library',
    hours: ['9–20', '9–20', '9–20', '9–20', '9–18', '10–16', '12–16'],
  },
  {
    name: 'Harbour Branch',
    hours: ['10–18', '10–18', null, '10–18', '10–18', '10–14', null],
  },
  {
    name: 'Hillside Branch',
    hours: [null, '13–19', '13–19', '13–19', '10–16', '10–14', null],
  },
];
