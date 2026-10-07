import { Router, type Response } from 'express';
import { z } from 'zod';
import { sendProblem } from '../http/problem.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import type { CardRenderer } from './card-renderer.js';
import { isDocumentNumber } from './document-number.js';
import type { DocumentRecord, DocumentRepository } from './document-repository.js';
import { etagOf, matchesEtag } from './etag.js';
import { findDocument } from './find-document.js';

export interface DocumentRouteDependencies {
  readonly documents: Pick<DocumentRepository, 'getById' | 'getByNumber'>;
  readonly cards: Pick<CardRenderer, 'render'>;
  readonly publicBaseUrl: string;
}

const documentId = z.uuid();

function toResource(document: DocumentRecord) {
  return {
    id: document.id,
    number: document.number,
    title: document.title,
    collection: document.collection,
    pageCount: document.pageCount,
    createdAt: document.createdAt.toISOString(),
  };
}

export function documentRoutes({ documents, cards, publicBaseUrl }: DocumentRouteDependencies) {
  const read = requireScope(Scopes.documentsRead);

  async function documentFromRoute(
    id: unknown,
    res: Response,
  ): Promise<DocumentRecord | undefined> {
    const parsed = parseInput(documentId, id, res);
    return parsed === undefined ? undefined : findDocument(documents, parsed, res);
  }

  return Router()
    .get<{ number: string }>('/documents/by-number/:number', read, async (req, res) => {
      const { number } = req.params;
      if (!isDocumentNumber(number)) {
        sendProblem(res, 400, 'Expected a document number such as HR-2024-001337.');
        return;
      }
      const document = await documents.getByNumber(number);
      if (!document) {
        sendProblem(res, 404, 'No document has this number.');
        return;
      }
      res.json(toResource(document));
    })
    .get('/documents/:id', read, async (req, res) => {
      const document = await documentFromRoute(req.params.id, res);
      if (document) {
        res.json(toResource(document));
      }
    })
    .get('/documents/:id/card', read, async (req, res) => {
      const document = await documentFromRoute(req.params.id, res);
      if (!document) {
        return;
      }
      const html = cards.render(document, publicBaseUrl);
      const etag = etagOf(html);
      res.set('ETag', etag).set('Cache-Control', 'private, no-cache');
      if (matchesEtag(req.get('if-none-match'), etag)) {
        res.status(304).end();
        return;
      }
      res.type('html').send(html);
    });
}
