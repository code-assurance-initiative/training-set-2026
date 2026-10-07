import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { createLogger } from '../../src/logger.js';
import { createTestApp } from '../support/test-app.js';

describe('request logging', () => {
  it('logs each request with its id and never the bearer token', async () => {
    const lines: string[] = [];
    const logger = createLogger('info', { write: (line: string) => lines.push(line) });
    const api = await createTestApp({ logger });
    const token = api.fullAccess.replace('Bearer ', '');

    const response = await request(api.app)
      .get('/api/skus')
      .set('Authorization', api.fullAccess)
      .expect(200);

    const entries = lines.map((line) => JSON.parse(line) as Record<string, unknown>);
    const completed = entries.find((entry) => entry.msg === 'request completed');
    expect(completed?.req).toMatchObject({ id: response.headers['x-request-id'] });
    expect(lines.join('\n')).not.toContain(token);
    expect(lines.join('\n')).toContain('[Redacted]');
  });
});
