import express, { Router, type RequestHandler } from 'express';
import { z } from 'zod';
import { sendProblem } from '../http/problem.js';
import { answerTextBody } from '../http/text-body.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import { flattenMetadata, type JsonValue } from './metadata-flattener.js';
import { readMetadata } from './metadata-importer.js';
import { readRetentionSchedule } from './retention-schedule-reader.js';
import { InvalidXmlError } from './xml-elements.js';

const previewQuery = z.object({ url: z.url({ protocol: /^https?$/ }) });

const xmlBody = express.text({ type: ['application/xml', 'text/xml'], limit: '1mb' });

const isXmlRefusal = (error: unknown): error is InvalidXmlError => error instanceof InvalidXmlError;

/** A handler that answers with `respond(read(xml))` for an XML request body. */
function xmlImport<T>(read: (xml: string) => T, respond: (value: T) => object): RequestHandler {
  return (req, res) =>
    answerTextBody(res, req.body, 'application/xml', (xml) => respond(read(xml)), isXmlRefusal);
}

export function importRoutes() {
  const write = requireScope(Scopes.documentsWrite);

  return Router()
    .get('/imports/preview', write, async (req, res) => {
      const query = parseInput(previewQuery, req.query, res);
      if (!query) {
        return;
      }
      const { url } = query;
      const response = await fetch(url, { method: 'HEAD', signal: AbortSignal.timeout(5_000) });
      res.json({
        status: response.status,
        contentType: response.headers.get('content-type'),
        contentLength: Number(response.headers.get('content-length') ?? Number.NaN) || null,
        lastModified: response.headers.get('last-modified'),
      });
    })
    .post(
      '/imports/metadata',
      write,
      xmlBody,
      xmlImport(readMetadata, (fields) => ({ fields })),
    )
    .post('/imports/metadata.json', write, (req, res) => {
      if (!req.is('application/json')) {
        sendProblem(res, 415, 'Send the metadata document as application/json.');
        return;
      }
      const fields = flattenMetadata(req.body as JsonValue);
      res.json({ fields: Object.fromEntries(fields) });
    })
    .post(
      '/imports/retention-schedule',
      write,
      xmlBody,
      xmlImport(readRetentionSchedule, (rules) => ({ rules })),
    );
}
