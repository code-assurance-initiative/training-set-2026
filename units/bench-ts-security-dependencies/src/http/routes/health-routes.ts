import { Router } from 'express';

/** Liveness for the depot server's supervisor; carries no data and needs no token. */
export function healthRoutes(): Router {
  const router = Router();
  router.get('/health', (_req, res) => {
    res.set('Cache-Control', 'no-store').json({ status: 'ok' });
  });
  return router;
}
