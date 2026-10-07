import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { loadConfig } from '../../src/config.js';
import { startService } from '../../src/server.js';
import { silentLogger } from '../support/silent-logger.js';
import { createTokenIssuer, testIssuer } from '../support/test-tokens.js';

describe('startService', () => {
  it('listens on the configured address until closed', async () => {
    const tokens = await createTokenIssuer();
    const config = loadConfig({
      NODE_ENV: 'test',
      PORT: '0',
      JWT_ISSUER: testIssuer,
      JWT_PUBLIC_KEY: tokens.publicKeyPem,
    });

    const service = await startService(config, silentLogger);
    const response = await request(`http://127.0.0.1:${String(service.port)}`).get('/health');
    await service.close();

    expect(response.status).toBe(200);
    expect(service.port).toBeGreaterThan(0);
  });
});
