import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { emailHost, smsHost } from '../support/test-config.js';
import {
  bookClass,
  createTestApp,
  registerMember,
  scheduleClass,
  type TestApp,
} from '../support/test-app.js';

describe('class reminders', () => {
  let api: TestApp;

  beforeEach(async () => {
    api = await createTestApp();
  });

  it('reminds a booked member once, by e-mail and SMS, and logs the deliveries', async () => {
    const memberId = await registerMember(api, { phone: '+4520123456' });
    const sessionId = await scheduleClass(api, { title: 'Spinning' });
    await bookClass(api, sessionId, memberId);

    expect(await api.services.jobs.reminders.run()).toBe(1);
    expect(await api.services.jobs.reminders.run()).toBe(0);

    const [email] = api.provider.to(emailHost);
    expect(email?.body).toMatchObject({
      from: 'classes@club.test',
      to: 'ada.lindqvist@example.net',
      subject: 'Reminder: Spinning',
    });
    expect(email?.authorization).toBe(`Bearer ${api.config.email.apiToken}`);
    expect(api.provider.to(smsHost).map((sms) => sms.body.to)).toEqual(['+4520123456']);
    const deliveries = (await api.db('reminder_deliveries').orderBy('channel')) as {
      channel: string;
    }[];
    expect(deliveries.map((delivery) => delivery.channel)).toEqual(['email', 'sms']);
  });

  it('does not text members without a phone number', async () => {
    const sessionId = await scheduleClass(api);
    await bookClass(api, sessionId, await registerMember(api));

    await api.services.jobs.reminders.run();

    expect(api.provider.to(emailHost)).toHaveLength(1);
    expect(api.provider.to(smsHost)).toHaveLength(0);
  });

  it('leaves classes outside the reminder window and cancelled bookings alone', async () => {
    const memberId = await registerMember(api);
    const later = await scheduleClass(api, { startsAt: '2026-10-10T17:00:00Z' });
    await bookClass(api, later, memberId);
    const soon = await scheduleClass(api);
    await bookClass(api, soon, await registerMember(api, { email: 'leaver@example.net' }));
    const leaverId = (await api.db('members').where({ email: 'leaver@example.net' }).first()) as {
      id: string;
    };
    await request(api.app)
      .post(`/api/members/${leaverId.id}/cancellation`)
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(await api.services.jobs.reminders.run()).toBe(0);
    expect(api.provider.sent).toHaveLength(0);
  });

  it('does not log a delivery the provider rejected', async () => {
    const sessionId = await scheduleClass(api);
    await bookClass(api, sessionId, await registerMember(api, { phone: '+4520123456' }));
    api.provider.status = 503;

    expect(await api.services.jobs.reminders.run()).toBe(1);

    expect(api.provider.sent).toHaveLength(2);
    expect(await api.db('reminder_deliveries').count({ count: '*' })).toEqual([{ count: 0 }]);
  });
});
