import { Router } from 'express';
import type { Linehaul } from '../../carriers/linehaul-client.js';
import type { DispatchService } from '../../dispatch/dispatch-service.js';
import { bundleLabels, type PrintableLabel } from '../../labels/label-archive.js';
import type { LabelRasteriser } from '../../labels/label-rasteriser.js';
import { requireScope, Scopes } from '../require-terminal.js';
import { ownRun } from './run-routes.js';

/** The print station's download: every label of a run, rasterised for the thermal printers. */
export function labelRoutes(
  dispatch: DispatchService,
  linehaul: Linehaul,
  rasteriser: LabelRasteriser,
): Router {
  const router = Router();

  router.get('/runs/:id/labels.zip', requireScope(Scopes.printLabels), async (req, res) => {
    const run = ownRun(dispatch, req.params.id, res);
    if (!run) {
      return;
    }
    const labels: PrintableLabel[] = [];
    for (const route of run.routes) {
      for (const trackingNumber of route.trackingNumbers) {
        const pdf = await linehaul.fetchLabel(trackingNumber);
        labels.push({ trackingNumber, route: route.vehicle, png: rasteriser.rasterise(pdf) });
      }
    }
    const archive = await bundleLabels(run.id, labels);
    res
      .type('application/zip')
      .attachment(`labels-${run.serviceDate}-${run.depotId}.zip`)
      .send(archive);
  });

  return router;
}
