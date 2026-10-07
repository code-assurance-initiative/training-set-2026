import express, { Router } from 'express';
import type { CarrierAccountManager } from '../../application/accounts/carrier-account-manager.js';
import type { LabelService } from '../../application/labels/label-service.js';
import { sendProblem } from '../problem.js';

/** Carrier tracking webhooks, authenticated by an HMAC signature over the raw body. */
export function webhookRoutes(labels: LabelService, accounts: CarrierAccountManager): Router {
  const router = Router();
  router.post(
    '/webhooks/tracking',
    express.raw({ type: 'application/json', limit: '16kb' }),
    (req, res) => {
      const payload = Buffer.isBuffer(req.body) ? req.body.toString('utf8') : '';
      const signature = req.get('x-signature') ?? '';
      if (!accounts.verifyWebhook(payload, signature)) {
        sendProblem(res, 401, 'The webhook signature is not valid.');
        return;
      }
      let event: unknown;
      try {
        event = JSON.parse(payload);
      } catch {
        sendProblem(res, 400, 'The webhook body is not valid JSON.');
        return;
      }
      if (typeof event !== 'object' || event === null) {
        sendProblem(res, 400, 'The webhook body must be an object.');
        return;
      }
      res.status(labels.applyTrackingEvent(event) ? 204 : 202).end();
    },
  );
  return router;
}
