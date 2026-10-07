import type { Response } from 'express';
import { sendProblem } from '../http/problem.js';
import type { DocumentRecord, DocumentRepository } from './document-repository.js';

/** The document with this id, or undefined after answering 404. */
export async function findDocument(
  documents: Pick<DocumentRepository, 'getById'>,
  id: string,
  res: Response,
): Promise<DocumentRecord | undefined> {
  const document = await documents.getById(id);
  if (!document) {
    sendProblem(res, 404, 'No document has this id.');
  }
  return document;
}
