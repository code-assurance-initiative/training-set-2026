import { pino } from 'pino';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { startJobs } from '../../src/jobs.js';
import { IntegrationBus, type IntegrationMessage } from '../../src/platform/integration-bus.js';
import { Outbox, OutboxDispatcher } from '../../src/platform/outbox.js';
import { silentLogger } from '../support/silent-logger.js';
import { createTestDatabase } from '../support/test-database.js';

describe('outbox', () => {
  it('publishes messages in the order they occurred and removes them once handled', async () => {
    const db = await createTestDatabase();
    const bus = new IntegrationBus();
    const received: IntegrationMessage[] = [];
    bus.subscribe('test.happened', (message) => {
      received.push(message);
      return Promise.resolve();
    });
    const outbox = new Outbox(db);
    await outbox.add('test.happened', { n: 2 }, new Date('2026-10-07T08:00:02Z'));
    await outbox.add('test.happened', { n: 1 }, new Date('2026-10-07T08:00:01Z'));
    await outbox.add('test.unsubscribed', { n: 3 }, new Date('2026-10-07T08:00:03Z'));

    expect(await new OutboxDispatcher(db, bus, silentLogger).dispatchPending()).toBe(3);

    expect(received.map((message) => message.payload)).toEqual([{ n: 1 }, { n: 2 }]);
    expect(received[0]?.occurredAt).toBe('2026-10-07T08:00:01.000Z');
    expect(await db('outbox_messages').count({ count: '*' })).toEqual([{ count: 0 }]);
  });

  it('keeps a message whose handler failed, for the next round', async () => {
    const db = await createTestDatabase();
    const bus = new IntegrationBus();
    bus.subscribe('test.happened', () => Promise.reject(new Error('handler down')));
    await new Outbox(db).add('test.happened', {}, new Date());

    await expect(new OutboxDispatcher(db, bus, silentLogger).dispatchPending()).rejects.toThrow(
      'handler down',
    );
    expect(await db('outbox_messages').count({ count: '*' })).toEqual([{ count: 1 }]);
  });
});

describe('background jobs', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('runs every job each round and carries on after a failure', async () => {
    vi.useFakeTimers();
    const calls: string[] = [];
    const lines: string[] = [];
    const logger = pino({ level: 'error' }, { write: (line: string) => lines.push(line) });
    const job = (name: string, fails = false) => ({
      [name === 'outbox' ? 'dispatchPending' : 'run']: () => {
        calls.push(name);
        return fails ? Promise.reject(new Error(`${name} failed`)) : Promise.resolve(0);
      },
    });
    const jobs = {
      outbox: job('outbox', true),
      reminders: job('reminders'),
      purgeLapsedMembers: job('purge'),
    } as unknown as Parameters<typeof startJobs>[0];

    const running = startJobs(jobs, 1_000, logger);
    await vi.advanceTimersByTimeAsync(2_000);
    await running.stop();

    expect(calls).toEqual(['outbox', 'reminders', 'purge', 'outbox', 'reminders', 'purge']);
    expect(lines).toHaveLength(2);
    expect(JSON.parse(lines[0] ?? '{}')).toMatchObject({
      job: 'outbox',
      msg: 'Background job failed',
    });
  });
});
