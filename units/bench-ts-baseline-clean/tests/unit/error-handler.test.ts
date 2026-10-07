import express from 'express';
import { pino } from 'pino';
import { pinoHttp } from 'pino-http';
import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { errorHandler } from '../../src/http/errors.js';

function appFailingWith(error: unknown, logged: string[] = []) {
  const app = express();
  app.use(
    pinoHttp({ logger: pino({ level: 'error' }, { write: (line: string) => logged.push(line) }) }),
  );
  app.get('/fail', () => {
    throw error;
  });
  app.use(errorHandler);
  return app;
}

describe('errorHandler', () => {
  it('answers an unexpected error with a generic 500 and logs it', async () => {
    const logged: string[] = [];

    const response = await request(appFailingWith(new Error('store exploded'), logged))
      .get('/fail')
      .expect(500);

    expect(response.body).toEqual({
      type: 'about:blank',
      title: 'Internal Server Error',
      status: 500,
      detail: 'The request could not be processed.',
    });
    expect(JSON.stringify(response.body)).not.toContain('store exploded');
    expect(logged.some((line) => line.includes('store exploded'))).toBe(true);
  });

  it('passes a client error status through with a fixed explanation', async () => {
    const response = await request(appFailingWith({ status: 415, message: 'internal detail' }))
      .get('/fail')
      .expect(415);

    expect(response.body).toMatchObject({ detail: 'The request body must be JSON.' });
  });

  it('describes an unlisted client error generically', async () => {
    const response = await request(appFailingWith({ status: 431 }))
      .get('/fail')
      .expect(431);

    expect(response.body).toMatchObject({ detail: 'The request could not be read.' });
  });

  it('treats a server error status as unexpected', async () => {
    await request(appFailingWith({ status: 503 }))
      .get('/fail')
      .expect(500);
  });
});
