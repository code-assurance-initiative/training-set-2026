import { useMemo, useState, type SubmitEvent } from 'react';
import { CheckboxField } from '../../components/CheckboxField';
import { TextField } from '../../components/TextField';
import { validateTitle, type ReadingListEntry } from './readingList';
import { useReadingList } from './useReadingList';

const READING_TIPS = [
  'Add the books you hear about at storytime or in a reading group.',
  'Move a book up when it is next on your pile.',
  'Search the catalogue to see whether a branch has it on the shelf.',
] as const;

interface ReadingListRowProps {
  readonly entry: ReadingListEntry;
  readonly first: boolean;
  readonly last: boolean;
  readonly onMove: (id: string, offset: -1 | 1) => void;
  readonly onSaveNote: (id: string, note: string) => void;
  readonly onFinished: (id: string) => void;
  readonly onRemove: (id: string) => void;
}

function ReadingListRow({
  entry,
  first,
  last,
  onMove,
  onSaveNote,
  onFinished,
  onRemove,
}: ReadingListRowProps) {
  const [draft, setDraft] = useState(entry.note);

  return (
    <li
      className={
        entry.finished ? 'reading-list__entry reading-list__entry--finished' : 'reading-list__entry'
      }
    >
      <p className="reading-list__book">
        <strong>{entry.title}</strong>
        {entry.author && ` by ${entry.author}`}
        {entry.finished && ' (finished)'}
      </p>
      <TextField
        label={
          <>
            Note<span className="visually-hidden"> on {entry.title}</span>
          </>
        }
        value={draft}
        onChange={setDraft}
      />
      <div className="reading-list__actions">
        <button
          type="button"
          className="button button--small"
          onClick={() => {
            onSaveNote(entry.id, draft);
          }}
        >
          Save note<span className="visually-hidden"> on {entry.title}</span>
        </button>
        <button
          type="button"
          className="button button--small"
          disabled={first}
          onClick={() => {
            onMove(entry.id, -1);
          }}
        >
          Move up<span className="visually-hidden"> {entry.title}</span>
        </button>
        <button
          type="button"
          className="button button--small"
          disabled={last}
          onClick={() => {
            onMove(entry.id, 1);
          }}
        >
          Move down<span className="visually-hidden"> {entry.title}</span>
        </button>
        {!entry.finished && (
          <button
            type="button"
            className="button button--small"
            onClick={() => {
              onFinished(entry.id);
            }}
          >
            Mark as finished<span className="visually-hidden">: {entry.title}</span>
          </button>
        )}
        <button
          type="button"
          className="button button--small button--secondary"
          onClick={() => {
            onRemove(entry.id);
          }}
        >
          Remove<span className="visually-hidden"> {entry.title}</span>
        </button>
      </div>
    </li>
  );
}

export function ReadingListPage() {
  const { entries, add, move, saveNote, markFinished, remove } = useReadingList();
  const [title, setTitle] = useState('');
  const [author, setAuthor] = useState('');
  const [titleError, setTitleError] = useState<string | null>(null);
  const [hideFinished, setHideFinished] = useState(false);

  const shown = useMemo(
    () => (hideFinished ? entries.filter((entry) => !entry.finished) : entries),
    [entries, hideFinished],
  );

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    const error = validateTitle(title);
    setTitleError(error);
    if (error) {
      return;
    }
    add(title.trim(), author.trim());
    setTitle('');
    setAuthor('');
  };

  return (
    <div className="reading-list">
      <h1>Reading list</h1>
      <p>Keep track of the books you want to read. Your list is stored in this browser only.</p>

      <section aria-labelledby="add-book-title">
        <h2 id="add-book-title">Add a book</h2>
        <form className="reading-list__form" noValidate onSubmit={handleSubmit}>
          <TextField label="Title" value={title} onChange={setTitle} />
          {titleError && <p className="form-field__error">{titleError}</p>}
          <TextField label="Author (optional)" value={author} onChange={setAuthor} />
          <button type="submit" className="button button--primary">
            Add to reading list
          </button>
        </form>
      </section>

      <section aria-labelledby="your-list-title">
        <h2 id="your-list-title">Your list</h2>
        <CheckboxField
          label="Hide finished books"
          checked={hideFinished}
          onChange={setHideFinished}
        />
        {shown.length === 0 ? (
          <p>Your reading list is empty.</p>
        ) : (
          <ol className="reading-list__entries">
            {shown.map((entry, index) => (
              <ReadingListRow
                key={index}
                entry={entry}
                first={index === 0}
                last={index === shown.length - 1}
                onMove={move}
                onSaveNote={saveNote}
                onFinished={markFinished}
                onRemove={remove}
              />
            ))}
          </ol>
        )}
      </section>

      <section aria-labelledby="reading-tips-title">
        <h2 id="reading-tips-title">Reading tips</h2>
        <ul className="reading-list__tips">
          {READING_TIPS.map((tip, index) => (
            <li key={index}>{tip}</li>
          ))}
        </ul>
      </section>
    </div>
  );
}
