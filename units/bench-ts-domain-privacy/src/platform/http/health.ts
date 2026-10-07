import { Router } from 'express';
import type { Knex } from 'knex';
import { sendProblem } from './problem.js';

/** Liveness (the process answers) and readiness (the database answers). */
export function healthRoutes(db: Knex): Router {
  return Router()
    .get('/health/live', (_req, res) => {
      res.json({ status: 'ok' });
    })
    .get('/health/ready', async (req, res) => {
      try {
        await db.raw('select 1');
        res.json({ status: 'ok' });
      } catch (error) {
        req.log.warn({ err: error }, 'Readiness check failed');
        sendProblem(res, 503, 'The database is not reachable.');
      }
    });
}
