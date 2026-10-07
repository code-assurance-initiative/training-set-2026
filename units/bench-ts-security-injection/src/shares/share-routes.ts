import { randomBytes } from 'node:crypto';
import express, { Router } from 'express';
import { z } from 'zod';
import type { DocumentRepository } from '../documents/document-repository.js';
import { readCookie } from '../http/cookies.js';
import { sendProblem } from '../http/problem.js';
import { callerOf, requireScope, Scopes } from '../http/scopes.js';
import { parseIdWith, parseInput } from '../http/validation.js';
import { findDocument } from '../documents/find-document.js';
import type { ShareGrants } from './share-grants.js';
import { hashSharePassword, verifySharePassword } from './share-password.js';
import type { ShareStore } from './share-store.js';

const createShareBody = z.strictObject({
  password: z.string().min(8).max(200),
  validDays: z.number().int().min(1).max(30),
});

const unlockBody = z.object({ password: z.string().min(1).max(200) });

const grantCookie = 'share_grant';
const shareToken = z.string().regex(/^[A-Za-z0-9_-]{32}$/);

export interface ShareRouteDependencies {
  readonly shares: Pick<ShareStore, 'create' | 'findActive'>;
  readonly grants: Pick<ShareGrants, 'issue' | 'allows'>;
  readonly documents: Pick<DocumentRepository, 'getById'>;
  readonly now: () => Date;
}

/** Creating share links (authenticated, under /api). */
export function shareAdminRoutes({ shares, documents, now }: ShareRouteDependencies) {
  const write = requireScope(Scopes.documentsWrite);

  return Router().post('/documents/:id/shares', write, async (req, res) => {
    const input = parseIdWith(req.params.id, createShareBody, req.body, res);
    const document = input && (await findDocument(documents, input.id, res));
    if (!input || !document) {
      return;
    }
    const { id, value: body } = input;
    const token = randomBytes(24).toString('base64url');
    const expiresAt = new Date(now().getTime() + body.validDays * 86_400_000);
    await shares.create({
      token,
      documentId: id,
      createdBy: callerOf(res),
      passwordHash: hashSharePassword(body.password),
      expiresAt,
    });
    res.status(201).json({ token, url: `/shares/${token}`, expiresAt: expiresAt.toISOString() });
  });
}

/** The public side of a share link: unlock it with its password, then read the document. */
export function sharePublicRoutes({ shares, grants, documents, now }: ShareRouteDependencies) {
  return Router()
    .post(
      '/shares/:token/unlock',
      express.urlencoded({ extended: false, limit: '4kb' }),
      async (req, res) => {
        const token = parseInput(shareToken, req.params.token, res);
        const body = token === undefined ? undefined : parseInput(unlockBody, req.body, res);
        if (token === undefined || !body) {
          return;
        }
        const link = await shares.findActive(token, now());
        if (!link || !verifySharePassword(body.password, link.passwordHash)) {
          sendProblem(res, 403, 'The link has expired or the password is wrong.');
          return;
        }
        res.cookie(grantCookie, grants.issue(token), {
          httpOnly: true,
          secure: true,
          sameSite: 'strict',
          path: `/shares/${token}`,
          maxAge: 15 * 60_000,
        });
        const next =
          typeof req.query.next === 'string' ? req.query.next : `/shares/${token}/document`;
        res.redirect(303, next);
      },
    )
    .get('/shares/:token/document', async (req, res) => {
      const token = parseInput(shareToken, req.params.token, res);
      if (token === undefined) {
        return;
      }
      const link = await shares.findActive(token, now());
      if (!link || !grants.allows(readCookie(req.get('cookie'), grantCookie), token)) {
        sendProblem(res, 403, 'Unlock the link with its password first.');
        return;
      }
      const document = await documents.getById(link.documentId);
      if (!document) {
        sendProblem(res, 404, 'The shared document no longer exists.');
        return;
      }
      res.json({ id: document.id, number: document.number, title: document.title });
    });
}
