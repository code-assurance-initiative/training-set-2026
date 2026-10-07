import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { usePreferences } from '../src/context/settings';
import { SettingsPage } from '../src/features/settings/SettingsPage';
import { renderWithProviders } from './support/renderWithProviders';

function PreferencesProbe() {
  const { preferences } = usePreferences();
  return <output>{JSON.stringify(preferences)}</output>;
}

describe('SettingsPage', () => {
  it('toggles the compact result list by click and by keyboard', async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <>
        <SettingsPage />
        <PreferencesProbe />
      </>,
    );
    const toggle = screen.getByRole('switch', { name: 'Compact result list' });

    await user.click(toggle);
    expect(screen.getByText(/"compactResults":true/)).toBeInTheDocument();

    toggle.focus();
    await user.keyboard(' ');
    expect(screen.getByText(/"compactResults":false/)).toBeInTheDocument();
  });

  it('saves reminder preferences and confirms', async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <>
        <SettingsPage />
        <PreferencesProbe />
      </>,
    );

    await user.selectOptions(
      screen.getByRole('combobox', { name: 'Days before the due date' }),
      '7',
    );
    await user.click(screen.getByRole('radio', { name: 'Harbour Branch' }));
    await user.click(screen.getByRole('button', { name: 'Save reminder preferences' }));

    expect(screen.getByText(/"reminderDays":7/)).toHaveTextContent('"homeBranch":"harbour"');
    expect(screen.getByText('Your reminder preferences were saved.')).toBeInTheDocument();
  });

  it('disables the reminder interval when email reminders are off', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SettingsPage />);

    await user.click(screen.getByRole('checkbox', { name: 'Email me before a loan is due' }));

    expect(screen.getByRole('combobox', { name: 'Days before the due date' })).toBeDisabled();
  });
});
