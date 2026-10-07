import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { Router } from 'express';
import { z } from 'zod';
import { sendProblem } from '../http/problem.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import { contentTypeOf } from './content-types.js';

const documentId = z.uuid();

function isMissingFile(error: unknown): boolean {
  return (
    error instanceof Error &&
    'code' in error &&
    (error.code === 'ENOENT' || error.code === 'EISDIR')
  );
}

/** Serves the files attached to a document, stored under `<root>/<document id>/<file name>`. */
export function attachmentRoutes(root: string) {
  const read = requireScope(Scopes.documentsRead);

  return Router().get<{ id: string; name: string }>(
    '/documents/:id/attachments/:name',
    read,
    async (req, res) => {
      const id = parseInput(documentId, req.params.id, res);
      if (id === undefined) {
        return;
      }
      const file = path.join(root, id, req.params.name);
      try {
        const content = await readFile(file);
        res.type(contentTypeOf(file)).attachment(path.basename(file)).send(content);
      } catch (error) {
        if (!isMissingFile(error)) {
          throw error;
        }
        sendProblem(res, 404, 'The document has no attachment with this name.');
      }
    },
  );
}
