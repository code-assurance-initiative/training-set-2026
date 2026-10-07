import { EventEmitter } from 'node:events';
import { pino } from 'pino';
import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { run } from '../../src/lifecycle.js';
import { createTokenIssuer } from '../support/test-tokens.js';
import { randomBytes } from 'node:crypto';

async function environment(): Promise<NodeJS.ProcessEnv> {
  const tokens = await createTokenIssuer();
  return {
    NODE_ENV: 'test',
    PORT: '0',
    LOG_LEVEL: 'silent',
    JWT_ISSUER: 'https://identity.club.test/realms/staff',
    JWT_PUBLIC_KEY: tokens.publicKeyPem,
    DATABASE_URL: 'postgres://club@127.0.0.1:1/club',
    FIELD_ENCRYPTION_KEY: randomBytes(32).toString('base64'),
    PSEUDONYM_KEY: randomBytes(32).toString('base64'),
    EMAIL_API_URL: 'https://mail.provider.test/v1/messages',
    EMAIL_API_TOKEN: 'configured-at-deploy',
    EMAIL_SENDER: 'classes@club.test',
    SMS_API_URL: 'https://sms.provider.test/v2/sms',
    SMS_API_TOKEN: 'configured-at-deploy',
    SMS_SENDER: 'Club',
    JOB_INTERVAL_SECONDS: '3600',
  };
}

describe('service lifecycle', () => {
  it('starts, answers liveness, and stops on SIGTERM', async () => {
    const signals = new EventEmitter();

    const service = await run(await environment(), signals);

    expect(service).toBeDefined();
    const port = service?.port ?? 0;
    await request(`http://127.0.0.1:${port}`).get('/health/live').expect(200);
    const stopped = new Promise((resolve) => setTimeout(resolve, 100));
    signals.emit('SIGTERM', 'SIGTERM');
    signals.emit('SIGINT', 'SIGINT');
    await stopped;
    await expect(request(`http://127.0.0.1:${port}`).get('/health/live')).rejects.toThrow();
  });

  it('logs why it cannot start and resolves to undefined', async () => {
    const lines: string[] = [];
    const bootLogger = pino({ level: 'info' }, { write: (line: string) => lines.push(line) });

    const service = await run({ NODE_ENV: 'test' }, new EventEmitter(), bootLogger);

    expect(service).toBeUndefined();
    expect(JSON.parse(lines[0] ?? '{}')).toMatchObject({
      level: 60,
      msg: 'Club membership service failed to start',
    });
  });
});
