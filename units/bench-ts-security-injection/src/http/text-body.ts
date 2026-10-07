import type { Response } from 'express';
import { sendProblem } from './problem.js';

/**
 * Answers with `handle(text)` for a request whose body is non-empty text (an XML or YAML document);
 * 415 when there is none, 400 when `handle` throws an error the caller classifies as a refusal of
 * the document. Any other error propagates to the error handler.
 */
export async function answerTextBody(
  res: Response,
  body: unknown,
  mediaType: string,
  handle: (text: string) => object | Promise<object>,
  isRefusal: (error: unknown) => error is Error,
): Promise<void> {
  if (typeof body !== 'string' || body.length === 0) {
    sendProblem(res, 415, `Send the document as ${mediaType}.`);
    return;
  }
  try {
    res.json(await handle(body));
  } catch (error) {
    if (!isRefusal(error)) {
      throw error;
    }
    sendProblem(res, 400, error.message);
  }
}
