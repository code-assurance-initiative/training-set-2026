import { Router } from 'express';
import { z } from 'zod';
import { sendProblem } from '../http/problem.js';
import { callerOf, requireScope, Scopes } from '../http/scopes.js';
import { parseIdWith } from '../http/validation.js';
import type { ExportService } from './export-service.js';

const createExportBody = z.strictObject({
  format: z.string().min(1).max(60),
  fileName: z.string().min(1).max(200),
  password: z.string().min(12).max(200).optional(),
});

const downloadBody = z.strictObject({ password: z.string().max(200).optional() });

export function exportRoutes(exports: Pick<ExportService, 'create' | 'download'>) {
  const write = requireScope(Scopes.exportsWrite);

  return Router()
    .post('/documents/:id/exports', write, async (req, res) => {
      const input = parseIdWith(req.params.id, createExportBody, req.body, res);
      if (!input) {
        return;
      }
      const exportId = await exports.create(callerOf(res), input.id, input.value);
      if (exportId === undefined) {
        sendProblem(res, 404, 'No document has this id.');
        return;
      }
      res.status(201).location(`/api/exports/${exportId}`).json({ id: exportId });
    })
    .post('/exports/:id/download', write, async (req, res) => {
      const input = parseIdWith(req.params.id, downloadBody, req.body ?? {}, res);
      if (!input) {
        return;
      }
      const result = await exports.download(callerOf(res), input.id, input.value.password);
      if (result.kind === 'not-found') {
        sendProblem(res, 404, 'No export has this id.');
        return;
      }
      if (result.kind === 'password-required') {
        sendProblem(res, 403, 'This export is protected by a password.');
        return;
      }
      res.download(result.record.filePath, result.record.fileName);
    });
}
