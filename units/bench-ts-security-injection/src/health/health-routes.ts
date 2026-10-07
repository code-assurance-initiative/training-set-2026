import { Router } from 'express';

export function healthRoutes() {
  return Router().get('/health', (_req, res) => {
    res.set('Cache-Control', 'no-store').json({ status: 'ok' });
  });
}
