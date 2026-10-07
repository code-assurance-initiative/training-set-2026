import { Router } from 'express';
import { sendProblem } from '../http/problem.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { TemplateNotFoundError, type TemplateStore } from './template-store.js';

export function templateRoutes(templates: Pick<TemplateStore, 'read'>) {
  const read = requireScope(Scopes.reportsRead);

  return Router().get<{ name: string }>('/templates/:name', read, async (req, res) => {
    try {
      const text = await templates.read(req.params.name);
      res.type('text/plain').send(text);
    } catch (error) {
      if (!(error instanceof TemplateNotFoundError)) {
        throw error;
      }
      sendProblem(res, 404, error.message);
    }
  });
}
