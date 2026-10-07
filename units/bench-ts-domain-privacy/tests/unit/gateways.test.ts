import { randomBytes } from 'node:crypto';
import { pino } from 'pino';
import { describe, expect, it } from 'vitest';
import { EmailAddress } from '../../src/membership/domain/members/email-address.js';
import { MemberId } from '../../src/membership/domain/members/member-id.js';
import { PhoneNumber } from '../../src/membership/domain/members/phone-number.js';
import { HttpEmailGateway } from '../../src/membership/features/send-class-reminders/email-gateway.js';
import { HttpSmsGateway } from '../../src/membership/features/send-class-reminders/sms-gateway.js';
import { createPseudonymiser } from '../../src/platform/pseudonym.js';
import { FakeProvider } from '../support/fake-provider.js';

function capturingLogger(): { logger: pino.Logger; lines: string[] } {
  const lines: string[] = [];
  const logger = pino({ level: 'info' }, { write: (line: string) => lines.push(line) });
  return { logger, lines };
}

const options = {
  endpoint: new URL('https://provider.test/send'),
  apiToken: randomBytes(16).toString('hex'),
  sender: 'Club',
};

describe('e-mail gateway', () => {
  it('posts the message with the API token', async () => {
    const provider = new FakeProvider();
    const { logger } = capturingLogger();
    const gateway = new HttpEmailGateway(
      options,
      createPseudonymiser(randomBytes(32)),
      logger,
      provider.fetch,
    );

    const accepted = await gateway.send({
      memberId: MemberId.create(),
      to: EmailAddress.of('ada.lindqvist@example.net'),
      subject: 'Reminder: Spinning',
      text: 'See you there',
    });

    expect(accepted).toBe(true);
    expect(provider.sent[0]).toMatchObject({
      authorization: `Bearer ${options.apiToken}`,
      body: { from: 'Club', to: 'ada.lindqvist@example.net', subject: 'Reminder: Spinning' },
    });
  });

  it('logs a rejected message under a pseudonym, never the address', async () => {
    const provider = new FakeProvider();
    provider.status = 422;
    const { logger, lines } = capturingLogger();
    const pseudonymise = createPseudonymiser(randomBytes(32));
    const memberId = MemberId.create();
    const gateway = new HttpEmailGateway(options, pseudonymise, logger, provider.fetch);

    const accepted = await gateway.send({
      memberId,
      to: EmailAddress.of('ada.lindqvist@example.net'),
      subject: 'Reminder',
      text: 'See you there',
    });

    expect(accepted).toBe(false);
    expect(lines).toHaveLength(1);
    expect(JSON.parse(lines[0] ?? '{}')).toMatchObject({
      member: pseudonymise(memberId.value),
      domain: 'example.net',
    });
    expect(lines[0]).not.toContain('ada.lindqvist');
    expect(lines[0]).not.toContain(memberId.value);
  });
});

describe('SMS gateway', () => {
  it('reports whether the provider accepted the message', async () => {
    const provider = new FakeProvider();
    const { logger, lines } = capturingLogger();
    const gateway = new HttpSmsGateway(options, logger, provider.fetch);

    expect(await gateway.send(PhoneNumber.of('+4520123456'), 'See you there')).toBe(true);
    provider.status = 500;
    expect(await gateway.send(PhoneNumber.of('+4520123456'), 'See you there')).toBe(false);

    expect(provider.sent.map((request) => request.body.to)).toEqual(['+4520123456', '+4520123456']);
    expect(lines).toHaveLength(1);
  });
});
