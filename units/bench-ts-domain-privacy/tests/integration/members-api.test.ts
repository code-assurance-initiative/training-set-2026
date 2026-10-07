import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuditLog } from '../../src/platform/audit-log.js';
import { createTestApp, registerMember, type TestApp } from '../support/test-app.js';

describe('members API', () => {
  let api: TestApp;

  beforeEach(async () => {
    api = await createTestApp();
  });

  it('registers a member and exports their data', async () => {
    const memberId = await registerMember(api, { phone: '+45 20 12 34 56' });

    const exported = await request(api.app)
      .get(`/api/members/${memberId}/personal-data`)
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(exported.body).toMatchObject({
      memberId,
      firstName: 'Ada',
      lastName: 'Lindqvist',
      email: 'ada.lindqvist@example.net',
      phone: '+4520123456',
      dateOfBirth: '1990-04-12',
      status: 'active',
      membershipEndsOn: '2026-12-31',
      consents: [],
      bookings: [],
    });
  });

  it('stores phone and date of birth only encrypted', async () => {
    const memberId = await registerMember(api, { phone: '+4520123456' });

    const row = (await api.db('members').where({ id: memberId }).first()) as Record<
      string,
      unknown
    >;

    expect(row.email).toBe('ada.lindqvist@example.net');
    expect(row.phone_encrypted).toMatch(/^v1:/);
    expect(JSON.stringify(row)).not.toContain('4520123456');
    expect(JSON.stringify(row)).not.toContain('1990-04-12');
  });

  it('refuses a second member with the same e-mail address', async () => {
    await registerMember(api);

    const response = await request(api.app)
      .post('/api/members')
      .set('Authorization', api.fullAccess)
      .send({
        firstName: 'Bo',
        lastName: 'Lindqvist',
        email: 'ADA.LINDQVIST@example.net',
        dateOfBirth: '1988-01-30',
        membershipEndsOn: '2026-12-31',
      })
      .expect(409);

    expect(response.body).toMatchObject({ status: 409 });
  });

  it('refuses members younger than sixteen', async () => {
    const response = await request(api.app)
      .post('/api/members')
      .set('Authorization', api.fullAccess)
      .send({
        firstName: 'Liv',
        lastName: 'Holm',
        email: 'liv.holm@example.net',
        dateOfBirth: '2012-02-01',
        membershipEndsOn: '2026-12-31',
      })
      .expect(400);

    expect(response.body).toMatchObject({ detail: 'Members must be at least 16 years old.' });
  });

  it('changes contact details and audits which fields changed', async () => {
    const memberId = await registerMember(api);

    await request(api.app)
      .put(`/api/members/${memberId}/contact-details`)
      .set('Authorization', api.fullAccess)
      .send({ email: 'ada@example.org', phone: '+4520987654' })
      .expect(200);

    const entries = await new AuditLog(api.db).forSubject('member', memberId);
    expect(entries.map((entry) => entry.action)).toEqual([
      'member-registered',
      'contact-details-changed',
    ]);
    expect(entries[1]?.changedFields).toEqual(['email', 'phone']);
    expect(entries[1]?.actor).toBe('user:front-desk-04');
    const exported = await request(api.app)
      .get(`/api/members/${memberId}/personal-data`)
      .set('Authorization', api.fullAccess);
    expect(exported.body).toMatchObject({ email: 'ada@example.org', phone: '+4520987654' });
  });

  it('refuses an e-mail address another member uses', async () => {
    await registerMember(api, { email: 'taken@example.net' });
    const memberId = await registerMember(api);

    await request(api.app)
      .put(`/api/members/${memberId}/contact-details`)
      .set('Authorization', api.fullAccess)
      .send({ email: 'taken@example.net' })
      .expect(409);
  });

  it('records, withdraws and audits consent', async () => {
    const memberId = await registerMember(api);

    for (const granted of [true, false]) {
      await request(api.app)
        .put(`/api/members/${memberId}/consents/sms-reminders`)
        .set('Authorization', api.fullAccess)
        .send({ granted })
        .expect(200, { purpose: 'sms-reminders', granted });
    }

    const exported = await request(api.app)
      .get(`/api/members/${memberId}/personal-data`)
      .set('Authorization', api.fullAccess);
    expect(exported.body).toMatchObject({
      consents: [{ purpose: 'sms-reminders', granted: false }],
    });
    const entries = await new AuditLog(api.db).forSubject('member', memberId);
    expect(entries.filter((entry) => entry.action === 'consent-recorded')).toHaveLength(2);
  });

  it('rejects an unknown consent purpose', async () => {
    const memberId = await registerMember(api);

    await request(api.app)
      .put(`/api/members/${memberId}/consents/profiling`)
      .set('Authorization', api.fullAccess)
      .send({ granted: true })
      .expect(400);
  });

  it('erases a member, and Billing anonymises its copy once the outbox is dispatched', async () => {
    const memberId = await registerMember(api);
    await api.services.jobs.outbox.dispatchPending();

    await request(api.app)
      .post(`/api/members/${memberId}/erasure`)
      .set('Authorization', api.fullAccess)
      .expect(200, { erased: true });
    await api.services.jobs.outbox.dispatchPending();

    const member = (await api.db('members').where({ id: memberId }).first()) as Record<
      string,
      unknown
    >;
    expect(member).toMatchObject({
      first_name: null,
      email: null,
      phone_encrypted: null,
      date_of_birth_encrypted: null,
      status: 'erased',
    });
    const account = (await api
      .db('billing_accounts')
      .where({ member_id: memberId })
      .first()) as Record<string, unknown>;
    expect(account).toMatchObject({ holder_name: null, holder_email: null, date_of_birth: null });
    await request(api.app)
      .get(`/api/members/${memberId}/personal-data`)
      .set('Authorization', api.fullAccess)
      .expect(404);
    await request(api.app)
      .post(`/api/members/${memberId}/erasure`)
      .set('Authorization', api.fullAccess)
      .expect(409);
    expect(await api.db('outbox_messages').count({ count: '*' })).toEqual([{ count: 0 }]);
  });

  it('answers 404 for members that do not exist', async () => {
    const unknown = '3f2b8a51-6c1d-4e0f-9a7b-2d5c8e1f4a60';
    for (const path of [
      `/api/members/${unknown}/erasure`,
      `/api/members/${unknown}/cancellation`,
    ]) {
      await request(api.app).post(path).set('Authorization', api.fullAccess).expect(404);
    }
    await request(api.app)
      .put(`/api/members/${unknown}/contact-details`)
      .set('Authorization', api.fullAccess)
      .send({ email: 'nobody@example.net' })
      .expect(404);
    await request(api.app)
      .put(`/api/members/${unknown}/consents/newsletter`)
      .set('Authorization', api.fullAccess)
      .send({ granted: true })
      .expect(404);
  });

  it('rejects a malformed member id', async () => {
    await request(api.app)
      .get('/api/members/not-a-uuid/personal-data')
      .set('Authorization', api.fullAccess)
      .expect(400);
  });
});
