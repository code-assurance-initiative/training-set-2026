import { Writable } from 'node:stream';
import { pino } from 'pino';
import { describe, expect, it } from 'vitest';
import { CrmClient, CrmError } from '../../src/subscriptions/crm-client.js';
import {
  HttpMailRelay,
  MailRelayError,
  type OutgoingMail,
} from '../../src/subscriptions/mail-transport.js';
import { pseudonymize } from '../../src/subscriptions/pseudonym.js';
import { ReportMailer } from '../../src/subscriptions/report-mailer.js';
import type { Subscription } from '../../src/subscriptions/subscription.js';
import { fakeFetch } from '../support/fake-fetch.js';

const key = 'a-test-pseudonym-key-that-is-long-enough';

function subscription(email: string): Subscription {
  return {
    _id: email,
    reportId: 'r',
    email,
    unsubscribeToken: `token-${email}`,
    createdAt: new Date(),
  };
}

function capturingLogger() {
  const lines: Record<string, unknown>[] = [];
  const destination = new Writable({
    write(chunk: Buffer, _encoding, callback) {
      lines.push(JSON.parse(chunk.toString()) as Record<string, unknown>);
      callback();
    },
  });
  return { logger: pino({ level: 'info' }, destination), lines };
}

describe('pseudonyms', () => {
  it('are stable per address, case-insensitive and keyed', () => {
    expect(pseudonymize('Ann@example.org', key)).toBe(pseudonymize(' ann@example.org', key));
    expect(pseudonymize('ann@example.org', key)).not.toBe(
      pseudonymize('ann@example.org', `${key}x`),
    );
    expect(pseudonymize('ann@example.org', key)).toHaveLength(22);
  });
});

describe('CRM client', () => {
  it('finds a contact and answers undefined for an unknown one', async () => {
    const { fetchImpl, calls } = fakeFetch(
      Response.json({ displayName: 'Ann Archivist' }),
      new Response('', { status: 404 }),
    );
    const crm = new CrmClient('https://crm.test/api', fetchImpl);

    await expect(crm.findContact('ann@example.org')).resolves.toEqual({
      displayName: 'Ann Archivist',
    });
    await expect(crm.findContact('bob@example.org')).resolves.toBeUndefined();
    expect(calls[0]?.url).toBe('https://crm.test/api/contacts?email=ann%40example.org');
  });

  it('switches the e-mail channel of a subscription', async () => {
    const { fetchImpl, calls } = fakeFetch(
      new Response(null, { status: 204 }),
      new Response('', { status: 500 }),
    );
    const crm = new CrmClient('https://crm.test/api', fetchImpl);

    await crm.setEmailChannel('sub 1', false);
    await expect(crm.setEmailChannel('sub 1', true)).rejects.toBeInstanceOf(CrmError);
    expect(calls[0]).toMatchObject({
      url: 'https://crm.test/api/subscriptions/sub%201/channels?email=false',
      init: { method: 'PUT' },
    });
  });

  it('reports a failing lookup', async () => {
    const { fetchImpl } = fakeFetch(new Response('', { status: 502 }));

    await expect(
      new CrmClient('https://crm.test/api', fetchImpl).findContact('a@b.c'),
    ).rejects.toBeInstanceOf(CrmError);
  });
});

describe('mail relay', () => {
  const mail: OutgoingMail = { to: 'ann@example.org', subject: 'Report', text: 'Hello' };

  it('posts the message as JSON and reads the outcome', async () => {
    const { fetchImpl, calls } = fakeFetch(
      new Response(null, { status: 202 }),
      new Response('', { status: 422 }),
      new Response('', { status: 500 }),
    );
    const relay = new HttpMailRelay('https://relay.test', fetchImpl);

    await expect(relay.send(mail)).resolves.toBe('delivered');
    await expect(relay.send(mail)).resolves.toBe('bounced');
    await expect(relay.send(mail)).rejects.toBeInstanceOf(MailRelayError);
    expect(calls[0]?.url).toBe('https://relay.test/v1/messages');
    expect(JSON.parse(calls[0]?.init?.body as string)).toEqual(mail);
  });
});

describe('report mailer', () => {
  it('greets each recipient by name, links the report and counts outcomes', async () => {
    const sent: OutgoingMail[] = [];
    const { logger, lines } = capturingLogger();
    const mailer = new ReportMailer(
      {
        send: (mail) => {
          sent.push(mail);
          return Promise.resolve(mail.to.startsWith('bounce') ? 'bounced' : 'delivered');
        },
      },
      {
        findContact: (email) =>
          Promise.resolve(email === 'ann@example.org' ? { displayName: 'Ann' } : undefined),
      },
      key,
      'https://archive.test',
      logger,
    );

    const summary = await mailer.deliver(
      { id: 'r', name: 'HR intake', link: 'https://archive.test/reports/r' },
      [subscription('ann@example.org'), subscription('bounce@example.org')],
    );

    expect(summary).toEqual({ delivered: 1, bounced: 1 });
    expect(sent[0]?.text).toContain('Hello Ann,');
    expect(sent[0]?.text).toContain('https://archive.test/unsubscribe?token=token-ann@example.org');
    expect(sent[1]?.text).toContain('Hello there,');
    expect(lines.map((line) => line.msg)).toEqual(['Report delivered', 'Report delivery bounced']);
    expect(lines[1]?.recipientRef).toBe(pseudonymize('bounce@example.org', key));
  });
});
