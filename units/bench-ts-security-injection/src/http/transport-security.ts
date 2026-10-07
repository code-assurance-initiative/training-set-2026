import type { RequestHandler } from 'express';
import { sendProblem } from './problem.js';

/**
 * Refuses requests that did not arrive over HTTPS. TLS terminates at the proxy in front of the
 * service; `req.secure` honours its forwarded protocol only for proxies named in TRUST_PROXY.
 */
export function requireHttps(enforced: boolean): RequestHandler {
  return (req, res, next) => {
    if (!enforced || req.secure) {
      next();
      return;
    }
    sendProblem(res, 403, 'This API is only served over HTTPS.');
  };
}
