import { describe, expect, it } from 'vitest';
import {
  departureOn,
  InvalidServiceDateError,
  windowForStop,
} from '../../src/scheduling/delivery-windows.js';

describe('departure', () => {
  it('is 08:00 local time on the service date', () => {
    const departure = departureOn('2026-11-02');
    expect([departure.getFullYear(), departure.getMonth(), departure.getDate()]).toEqual([
      2026, 10, 2,
    ]);
    expect([departure.getHours(), departure.getMinutes()]).toEqual([8, 0]);
  });

  it.each(['2026-11-07', '2026-11-08'])('refuses the weekend day %s', (date) => {
    expect(() => departureOn(date)).toThrow(/weekend/);
  });

  it.each(['2026-02-30', '2026-13-01', '02-11-2026', 'tomorrow'])('refuses %s', (date) => {
    expect(() => departureOn(date)).toThrow(InvalidServiceDateError);
  });
});

describe('delivery window', () => {
  const departure = departureOn('2026-11-02');

  it('opens on the hour of the expected arrival and lasts two hours', () => {
    expect(windowForStop(departure, 0)).toEqual({
      from: '2026-11-02 08:00',
      to: '2026-11-02 10:00',
    });
    expect(windowForStop(departure, 4)).toEqual({
      from: '2026-11-02 09:00',
      to: '2026-11-02 11:00',
    });
  });

  it('moves later along the route', () => {
    expect(windowForStop(departure, 39)).toEqual({
      from: '2026-11-02 16:00',
      to: '2026-11-02 18:00',
    });
  });

  it('follows the policy it is given', () => {
    const policy = { departure: '07:30', minutesPerStop: 30, windowMinutes: 60 };
    expect(windowForStop(departureOn('2026-11-03', policy), 1, policy)).toEqual({
      from: '2026-11-03 08:00',
      to: '2026-11-03 09:00',
    });
  });
});
