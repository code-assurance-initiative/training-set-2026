import { randomUUID } from 'node:crypto';
import { mkdir, readFile, rm } from 'node:fs/promises';
import path from 'node:path';
import { Router } from 'express';
import { z } from 'zod';
import type { DocumentRepository } from '../documents/document-repository.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseIdWith } from '../http/validation.js';
import { findDocument } from '../documents/find-document.js';
import { thumbnailSizes, type ThumbnailRenderer } from './thumbnail-renderer.js';

const thumbnailQuery = z.object({ size: z.enum(thumbnailSizes).default('medium') });

export interface ThumbnailRouteDependencies {
  readonly documents: Pick<DocumentRepository, 'getById'>;
  readonly renderer: Pick<ThumbnailRenderer, 'render'>;
  readonly originals: string;
  readonly scratch: string;
}

export function thumbnailRoutes({
  documents,
  renderer,
  originals,
  scratch,
}: ThumbnailRouteDependencies) {
  const read = requireScope(Scopes.documentsRead);

  return Router().get('/documents/:id/thumbnail', read, async (req, res) => {
    const input = parseIdWith(req.params.id, thumbnailQuery, req.query, res);
    const document = input && (await findDocument(documents, input.id, res));
    if (!input || !document) {
      return;
    }
    await mkdir(scratch, { recursive: true });
    const output = path.join(scratch, `${randomUUID()}.png`);
    try {
      await renderer.render(path.join(originals, document.storagePath), output, input.value.size);
      res
        .type('png')
        .set('Cache-Control', 'private, max-age=3600')
        .send(await readFile(output));
    } finally {
      await rm(output, { force: true });
    }
  });
}
