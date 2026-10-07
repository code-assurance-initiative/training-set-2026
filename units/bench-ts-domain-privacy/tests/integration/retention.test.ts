import { beforeEach, describe, expect, it } from 'vitest';
import { AuditLog } from '../../src/platform/audit-log.js';
import { createTestApp, registerMember, type TestApp } from '../support/test-app.js';

describe('retention of lapsed members', () => {
  let api: TestApp;

  beforeEach(async () => {
    api = await createTestApp();
  });

  it('erases members whose paid period ended more than the retention period ago', async () => {
    const lapsed = await registerMember(api, {
      email: 'lapsed@example.net',
      membershipEndsOn: '2026-10-31',
    });
    const current = await registerMember(api, {
      email: 'current@example.net',
      membershipEndsOn: '2028-12-31',
    });
    api.clock.set(new Date('2028-11-02T03:00:00Z'));

    expect(await api.services.jobs.purgeLapsedMembers.run()).toBe(1);
    expect(await api.services.jobs.purgeLapsedMembers.run()).toBe(0);

    const statuses = await api
      .db<{ id: string; status: string; email: string | null }>('members')
      .select('id', 'status', 'email');
    expect(statuses).toEqual(
      expect.arrayContaining([
        { id: lapsed, status: 'erased', email: null },
        { id: current, status: 'active', email: 'current@example.net' },
      ]),
    );
    const audit = await new AuditLog(api.db).forSubject('member', lapsed);
    expect(audit.at(-1)).toMatchObject({
      action: 'member-erased',
      actor: 'job:purge-lapsed-members',
    });
  });

  it('keeps members who lapsed within the retention period', async () => {
    await registerMember(api, { membershipEndsOn: '2026-10-31' });
    api.clock.set(new Date('2028-10-30T03:00:00Z'));

    expect(await api.services.jobs.purgeLapsedMembers.run()).toBe(0);
  });
});
