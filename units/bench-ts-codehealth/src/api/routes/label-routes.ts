import { Router } from 'express';
import type { LabelRequest, LabelService } from '../../application/labels/label-service.js';
import { parseInput, shipmentIdParam } from '../schemas.js';
import { requireScope, Scopes } from '../scopes.js';

export function labelRoutes(labels: LabelService): Router {
  const router = Router();

  router.post('/labels', requireScope(Scopes.labelsWrite), async (req, res) => {
    const created = await labels.createLabel(req.body as LabelRequest);
    labels.archive(created.shipmentId);
    res
      .status(201)
      .location(`/api/labels/${created.shipmentId}`)
      .json({ ...created, label: `/api/labels/${created.shipmentId}` });
  });

  router.get('/labels/:id', requireScope(Scopes.labelsWrite), async (req, res) => {
    const id = parseInput(shipmentIdParam, req.params.id, res);
    if (id !== undefined) {
      res.type('application/zpl').send(await labels.reprint(id));
    }
  });

  router.delete('/labels/:id', requireScope(Scopes.labelsWrite), async (req, res) => {
    const id = parseInput(shipmentIdParam, req.params.id, res);
    if (id !== undefined) {
      await labels.voidLabel(id);
      res.status(204).end();
    }
  });

  router.get('/statistics/labels', requireScope(Scopes.labelsWrite), (_req, res) => {
    res.json(labels.statistics());
  });

  return router;
}
