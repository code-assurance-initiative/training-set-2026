import { useId, useState, type SubmitEvent, type KeyboardEvent } from 'react';
import { BRANCHES, type Branch } from '../../api/types';
import { CheckboxField } from '../../components/CheckboxField';
import { usePreferences } from '../../context/settings';
import { useToasts } from '../../context/toasts';

const REMINDER_DAYS = [1, 2, 3, 5, 7] as const;

export function SettingsPage() {
  const { preferences, update } = usePreferences();
  const { notify } = useToasts();
  const [emailReminders, setEmailReminders] = useState(preferences.emailReminders);
  const [reminderDays, setReminderDays] = useState(preferences.reminderDays);
  const [homeBranch, setHomeBranch] = useState<Branch>(preferences.homeBranch);
  const compactLabelId = useId();
  const daysId = useId();

  const toggleCompact = () => {
    update({ compactResults: !preferences.compactResults });
  };

  const handleSwitchKey = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === ' ' || event.key === 'Enter') {
      event.preventDefault();
      toggleCompact();
    }
  };

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    update({ emailReminders, reminderDays, homeBranch });
    notify('success', 'Your reminder preferences were saved.');
  };

  return (
    <div className="settings">
      <h1>Settings</h1>
      <p>Preferences are stored in this browser only.</p>

      <h3>Search results</h3>
      <div className="setting">
        <span id={compactLabelId} className="setting__label">
          Compact result list
        </span>
        <div
          role="switch"
          tabIndex={0}
          aria-labelledby={compactLabelId}
          className={preferences.compactResults ? 'switch switch--on' : 'switch'}
          onClick={toggleCompact}
          onKeyDown={handleSwitchKey}
        >
          <span className="switch__thumb" />
        </div>
      </div>

      <h3>Loan reminders</h3>
      <form className="settings__form" onSubmit={handleSubmit}>
        <CheckboxField
          label="Email me before a loan is due"
          checked={emailReminders}
          onChange={setEmailReminders}
        />
        <div className="form-field">
          <label htmlFor={daysId}>Days before the due date</label>
          <select
            id={daysId}
            value={reminderDays}
            disabled={!emailReminders}
            onChange={(event) => {
              setReminderDays(Number(event.target.value));
            }}
          >
            {REMINDER_DAYS.map((days) => (
              <option key={days} value={days}>
                {days === 1 ? '1 day' : `${days} days`}
              </option>
            ))}
          </select>
        </div>
        <fieldset className="form-field">
          <legend>Home branch</legend>
          {Object.entries(BRANCHES).map(([value, name]) => (
            <label key={value} className="radio">
              <input
                type="radio"
                name="home-branch"
                value={value}
                checked={homeBranch === value}
                onChange={() => {
                  setHomeBranch(value as Branch);
                }}
              />
              {name}
            </label>
          ))}
        </fieldset>
        <button type="submit" className="button button--primary">
          Save reminder preferences
        </button>
      </form>
    </div>
  );
}
