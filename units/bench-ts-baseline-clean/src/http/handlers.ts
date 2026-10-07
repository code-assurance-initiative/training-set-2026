import type { RequestHandler } from 'express';
import type { z } from 'zod';
import type { Page, PageRequest } from '../application/paging.js';
import type { Result } from '../application/result.js';
import { sendResult } from './problem.js';
import { pageQuery, parseInput } from './schemas.js';

/** Answers a collection request with one page, parsed from the `offset` and `limit` query. */
export function listPage<T>(list: (request: PageRequest) => Page<T>): RequestHandler {
  return (req, res) => {
    const page = parseInput(pageQuery, req.query, res);
    if (page) {
      res.json(list(page));
    }
  };
}

/** Parses the path parameter `name` with `schema` and answers with the result of `operation`. */
export function withParam<T, V>(
  name: string,
  schema: z.ZodType<T>,
  operation: (value: T) => Result<V>,
): RequestHandler {
  return (req, res) => {
    const value = parseInput(schema, req.params[name], res);
    if (value !== undefined) {
      sendResult(res, operation(value));
    }
  };
}

/** Parses the body with `schema`, creates the resource and answers 201 with its location. */
export function createFromBody<T, V>(
  schema: z.ZodType<T>,
  create: (body: T) => Result<V>,
  locationOf: (created: V) => string,
): RequestHandler {
  return (req, res) => {
    const body = parseInput(schema, req.body, res);
    if (body !== undefined) {
      sendResult(res, create(body), locationOf);
    }
  };
}
