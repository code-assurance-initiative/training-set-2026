import express, { Router } from 'express';
import { parseStatusFeed, StatusFeedError } from '../../carriers/status-feed.js';
import type { DispatchStore } from '../../dispatch/dispatch-store.js';
import type { CarrierSignatureVerifier } from '../../webhooks/partner-signature.js';
import { sendProblem } from '../problem.js';

/**
 * The carrier pushes parcel status changes here. There is no bearer token: each body carries the
 * carrier's Ed25519 signature in `X-Signature`, verified over the raw bytes before anything is parsed.
 */
export function webhookRoutes(store: DispatchStore, signatures: CarrierSignatureVerifier): Router {
  const router = Router();

  router.post(
    '/webhooks/carrier-status',
    express.raw({ type: ['application/xml', 'text/xml'], limit: '256kb' }),
    async (req, res) => {
      const body: unknown = req.body;
      if (!Buffer.isBuffer(body)) {
        sendProblem(res, 415, 'The status feed must be sent as XML.');
        return;
      }
      if (!signatures.verify(new Uint8Array(body), req.get('x-signature'))) {
        sendProblem(res, 401, 'The status feed signature does not verify.');
        return;
      }
      try {
        const events = await parseStatusFeed(body.toString('utf8'));
        const applied = events.filter((event) =>
          store.updateStopStatus(event.trackingNumber, event.status),
        );
        res.status(202).json({ received: events.length, applied: applied.length });
      } catch (error) {
        if (error instanceof StatusFeedError) {
          sendProblem(res, 400, error.message);
          return;
        }
        throw error;
      }
    },
  );

  return router;
}
