export interface LibraryEvent {
  readonly id: string;
  readonly title: string;
  readonly date: string;
  readonly time: string;
  readonly branch: string;
  readonly audience: string;
}

export const UPCOMING_EVENTS: readonly LibraryEvent[] = [
  {
    id: 'storytime-oct-14',
    title: 'Storytime: autumn tales',
    date: '2026-10-14',
    time: '10:30',
    branch: 'Central Library',
    audience: 'Ages 2–5 with a grown-up',
  },
  {
    id: 'coding-club-oct-15',
    title: 'Coding club: build a weather station',
    date: '2026-10-15',
    time: '16:00',
    branch: 'Harbour Branch',
    audience: 'Ages 12–16',
  },
  {
    id: 'history-talk-oct-21',
    title: 'Talk: the old ferry crossing',
    date: '2026-10-21',
    time: '19:00',
    branch: 'Central Library',
    audience: 'Adults',
  },
];
