import { Router } from 'express';
import { z } from 'zod';
import { sendProblem } from '../http/problem.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import {
  FeedNotAllowedError,
  FeedUnavailableError,
  type PartnerFeedClient,
} from './partner-feed-client.js';

const feedQuery = z.object({ url: z.url() });

export function feedRoutes(feeds: Pick<PartnerFeedClient, 'entries'>) {
  const read = requireScope(Scopes.documentsRead);

  return Router().get('/feeds/entries', read, async (req, res) => {
    const query = parseInput(feedQuery, req.query, res);
    if (!query) {
      return;
    }
    try {
      res.json({ entries: await feeds.entries(query.url) });
    } catch (error) {
      if (error instanceof FeedNotAllowedError) {
        sendProblem(res, 400, error.message);
        return;
      }
      if (error instanceof FeedUnavailableError) {
        sendProblem(res, 502, error.message);
        return;
      }
      throw error;
    }
  });
}
