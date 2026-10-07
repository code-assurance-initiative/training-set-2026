import { Router } from 'express';

/** The only anonymous route: liveness for the proxy and orchestrator in front of the service. */
export function healthRoutes(): Router {
  const router = Router();
  router.get('/health', (_req, res) => {
    res.set('Cache-Control', 'no-store').json({ status: 'ok' });
  });
  return router;
}
