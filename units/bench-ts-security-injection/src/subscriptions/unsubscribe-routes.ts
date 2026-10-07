import { Router } from 'express';
import { sendProblem } from '../http/problem.js';
import type { SubscriptionCollection } from './subscription.js';

interface UnsubscribeRequest {
  readonly email: string;
  readonly token: string;
}

/**
 * Public: the unsubscribe page posts the address and the token from the mail's link. No sign-in;
 * the token is the proof that the caller received the mail.
 */
export function unsubscribeRoutes(subscriptions: Pick<SubscriptionCollection, 'deleteOne'>) {
  return Router().post('/unsubscribe', async (req, res) => {
    const { email, token } = req.body as UnsubscribeRequest;
    if (!email || !token) {
      sendProblem(res, 400, 'Both the address and the token from the mail are required.');
      return;
    }
    const result = await subscriptions.deleteOne({ email, unsubscribeToken: token });
    if (result.deletedCount === 0) {
      sendProblem(res, 404, 'No subscription matches this address and token.');
      return;
    }
    res.status(204).end();
  });
}
