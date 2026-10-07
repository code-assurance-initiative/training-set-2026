import { useState } from 'react';
import { getBoard, startRoute } from '../../api/dispatch';
import { ErrorMessage } from '../../components/ErrorMessage';
import {
  loadBoardDensity,
  saveBoardDensity,
  type BoardDensity,
} from '../../preferences/boardDensity';
import { useLoad } from '../../useLoad';
import { RouteCard } from './RouteCard';

const today = () => new Date().toISOString().slice(0, 10);

export function DispatchBoard() {
  const [date, setDate] = useState(today);
  const [board, reload] = useLoad((signal) => getBoard(date, signal), date);
  const [actionError, setActionError] = useState<unknown>();
  const [density, setDensity] = useState<BoardDensity>(loadBoardDensity);
  const [postalCode, setPostalCode] = useState('');

  const changeDensity = (next: BoardDensity) => {
    setDensity(next);
    saveBoardDensity(next);
  };

  const start = (routeId: string) => {
    startRoute(routeId).then(
      () => {
        setActionError(undefined);
        reload();
      },
      (error: unknown) => {
        setActionError(error);
      },
    );
  };

  return (
    <section aria-labelledby="board-heading">
      <h1 id="board-heading">Dispatch board</h1>
      <label htmlFor="board-date">Day</label>{' '}
      <input
        id="board-date"
        type="date"
        value={date}
        onChange={(event) => {
          setDate(event.target.value);
        }}
      />
      <fieldset className="density">
        <legend>Cards</legend>
        <label>
          <input
            type="radio"
            name="density"
            checked={density === 'comfortable'}
            onChange={() => {
              changeDensity('comfortable');
            }}
          />{' '}
          Comfortable
        </label>
        <label>
          <input
            type="radio"
            name="density"
            checked={density === 'compact'}
            onChange={() => {
              changeDensity('compact');
            }}
          />{' '}
          Compact
        </label>
      </fieldset>
      <h2 className="stops-heading">Stops by postal code</h2>
      <input
        type="search"
        aria-label="Show only stops with this postal code"
        value={postalCode}
        onChange={(event) => {
          setPostalCode(event.target.value.trim());
        }}
      />
      {actionError !== undefined && <ErrorMessage error={actionError} />}
      {board.state === 'loading' && <p role="status">Loading routes…</p>}
      {board.state === 'failed' && <ErrorMessage error={board.error} />}
      {board.state === 'loaded' &&
        (board.value.length === 0 ? (
          <p>No routes planned for this day.</p>
        ) : (
          <ul className={`board board-${density}`}>
            {board.value.map((route) => (
              <RouteCard
                key={route.routeId}
                route={route}
                postalCode={postalCode}
                onStart={start}
              />
            ))}
          </ul>
        ))}
    </section>
  );
}
