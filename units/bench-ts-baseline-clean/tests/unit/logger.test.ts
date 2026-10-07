import { describe, expect, it } from 'vitest';
import { systemClock } from '../../src/application/clock.js';
import { createLogger } from '../../src/logger.js';

describe('createLogger', () => {
  it('writes structured JSON with the service name', () => {
    const lines: string[] = [];
    const logger = createLogger('info', { write: (line: string) => lines.push(line) });

    logger.info({ skuCode: 'BOLT-M8-40' }, 'Registered SKU');

    expect(JSON.parse(lines[0] ?? '{}')).toMatchObject({
      level: 30,
      service: 'warehouse-stock-api',
      skuCode: 'BOLT-M8-40',
      msg: 'Registered SKU',
    });
  });

  it('logs at the configured level', () => {
    const logger = createLogger('warn');

    expect(logger.level).toBe('warn');
    expect(logger.isLevelEnabled('info')).toBe(false);
  });
});

describe('systemClock', () => {
  it('reads the current time', () => {
    const before = Date.now();

    const now = systemClock.now().getTime();

    expect(now).toBeGreaterThanOrEqual(before);
    expect(now).toBeLessThanOrEqual(Date.now());
  });
});
