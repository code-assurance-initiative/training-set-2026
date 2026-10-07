import { Router, type Response } from 'express';
import type { Linehaul } from '../../carriers/linehaul-client.js';
import type { DispatchRun } from '../../dispatch/dispatch-run.js';
import type { DispatchService } from '../../dispatch/dispatch-service.js';
import { InvalidServiceDateError } from '../../scheduling/delivery-windows.js';
import { sendProblem } from '../problem.js';
import { requireScope, Scopes, terminalOf } from '../require-terminal.js';
import { createRunSchema, describeIssues, runIdSchema } from '../schemas.js';

/** Dispatch runs of the calling terminal's own depot. */
export function runRoutes(dispatch: DispatchService, linehaul: Linehaul): Router {
  const router = Router();

  router.post('/runs', requireScope(Scopes.writeRuns), async (req, res) => {
    const parsed = createRunSchema.safeParse(req.body);
    if (!parsed.success) {
      sendProblem(res, 400, describeIssues(parsed.error));
      return;
    }
    if (parsed.data.depotId !== terminalOf(res).depotId) {
      sendProblem(res, 403, 'A terminal can only plan runs for its own depot.');
      return;
    }
    try {
      const result = await dispatch.createRun(parsed.data);
      if (!result.ok) {
        sendProblem(
          res,
          422,
          result.problem,
          result.unmatched ? { unmatched: result.unmatched } : {},
        );
        return;
      }
      res.status(201).location(`/api/runs/${result.run.id}`).json(result.run);
    } catch (error) {
      if (error instanceof InvalidServiceDateError) {
        sendProblem(res, 400, error.message);
        return;
      }
      throw error;
    }
  });

  router.get('/runs/:id', requireScope(Scopes.readRuns), (req, res) => {
    const run = ownRun(dispatch, req.params.id, res);
    if (run) {
      res.json(run);
    }
  });

  router.post('/runs/:id/quote', requireScope(Scopes.writeRuns), async (req, res) => {
    const run = ownRun(dispatch, req.params.id, res);
    if (!run) {
      return;
    }
    const quote = await linehaul.quote({
      depotId: run.depotId,
      serviceDate: run.serviceDate,
      parcels: run.stops.length,
      totalWeightKg: run.routes.reduce((sum, route) => sum + route.totalWeightKg, 0),
    });
    res.json(quote);
  });

  router.get('/runs/:id/export', requireScope(Scopes.readRuns), (req, res) => {
    const run = ownRun(dispatch, req.params.id, res);
    if (run) {
      res.attachment(`run-${run.serviceDate}-${run.depotId}.json`).json(run);
    }
  });

  return router;
}

/** The run with this id when it belongs to the caller's depot; otherwise answers 404 and returns undefined. */
export function ownRun(
  dispatch: DispatchService,
  id: unknown,
  res: Response,
): DispatchRun | undefined {
  const parsed = runIdSchema.safeParse(id);
  const run = parsed.success ? dispatch.find(parsed.data) : undefined;
  if (run?.depotId !== terminalOf(res).depotId) {
    sendProblem(res, 404, 'No such dispatch run at this depot.');
    return undefined;
  }
  return run;
}
