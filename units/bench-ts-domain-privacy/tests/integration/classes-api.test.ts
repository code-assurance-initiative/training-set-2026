import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import {
  bookClass,
  createTestApp,
  registerMember,
  scheduleClass,
  type TestApp,
} from '../support/test-app.js';

describe('classes and bookings API', () => {
  let api: TestApp;

  beforeEach(async () => {
    api = await createTestApp();
  });

  it('books a place and lists it among the member’s upcoming classes', async () => {
    const memberId = await registerMember(api);
    const sessionId = await scheduleClass(api, { title: 'Morning yoga' });

    const bookingId = await bookClass(api, sessionId, memberId);

    const upcoming = await request(api.app)
      .get(`/api/members/${memberId}/bookings`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    expect(upcoming.body).toEqual([
      {
        bookingId,
        memberId,
        sessionId,
        title: 'Morning yoga',
        startsAt: '2026-10-07T18:00:00.000Z',
      },
    ]);
  });

  it('refuses a second booking of the same class by the same member', async () => {
    const memberId = await registerMember(api);
    const sessionId = await scheduleClass(api);
    await bookClass(api, sessionId, memberId);

    await request(api.app)
      .post(`/api/classes/${sessionId}/bookings`)
      .set('Authorization', api.fullAccess)
      .send({ memberId })
      .expect(409);
  });

  it('refuses a booking when the class is full', async () => {
    const sessionId = await scheduleClass(api, { capacity: 1 });
    await bookClass(api, sessionId, await registerMember(api, { email: 'first@example.net' }));
    const second = await registerMember(api, { email: 'second@example.net' });

    const response = await request(api.app)
      .post(`/api/classes/${sessionId}/bookings`)
      .set('Authorization', api.fullAccess)
      .send({ memberId: second })
      .expect(409);
    expect(response.body).toMatchObject({ detail: 'The class is full.' });
  });

  it('refuses a booking of a class that has started', async () => {
    const memberId = await registerMember(api);
    const sessionId = await scheduleClass(api, { startsAt: '2026-10-07T07:30:00Z' });

    const response = await request(api.app)
      .post(`/api/classes/${sessionId}/bookings`)
      .set('Authorization', api.fullAccess)
      .send({ memberId })
      .expect(409);
    expect(response.body).toMatchObject({ detail: 'The class has already started.' });
  });

  it('refuses members whose paid period has ended', async () => {
    const memberId = await registerMember(api, { membershipEndsOn: '2026-10-07' });
    const sessionId = await scheduleClass(api);
    api.clock.advanceDays(1);

    const response = await request(api.app)
      .post(`/api/classes/${sessionId}/bookings`)
      .set('Authorization', api.fullAccess)
      .send({ memberId })
      .expect(409);
    expect(response.body).toMatchObject({
      detail: 'Only members in good standing can book classes.',
    });
  });

  it('answers 404 for an unknown class', async () => {
    const memberId = await registerMember(api);

    await request(api.app)
      .post('/api/classes/0d9c3b7e-2a41-4f6b-8e5d-7c1a9b3f2e40/bookings')
      .set('Authorization', api.fullAccess)
      .send({ memberId })
      .expect(404);
  });

  it('validates a class before scheduling it', async () => {
    await request(api.app)
      .post('/api/classes')
      .set('Authorization', api.fullAccess)
      .send({ title: '', startsAt: 'tomorrow', durationMinutes: 5, capacity: 0 })
      .expect(400);
  });

  it('cancels a membership and releases the member’s future places', async () => {
    const memberId = await registerMember(api);
    const sessionId = await scheduleClass(api, { capacity: 1 });
    await bookClass(api, sessionId, memberId);

    await request(api.app)
      .post(`/api/members/${memberId}/cancellation`)
      .set('Authorization', api.fullAccess)
      .expect(200, { releasedBookings: 1 });

    const other = await registerMember(api, { email: 'next@example.net' });
    await bookClass(api, sessionId, other);
    await request(api.app)
      .post(`/api/members/${memberId}/cancellation`)
      .set('Authorization', api.fullAccess)
      .expect(409);
  });
});
