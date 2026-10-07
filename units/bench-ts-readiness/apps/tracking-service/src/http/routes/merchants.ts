import { Router } from "express";
import { merchantParams, statsQuery } from "../schemas.js";
import type { TrackingService } from "../../parcels/tracking-service.js";

/** Delivery statistics per merchant, used by the merchant dashboard. */
export function merchantsRouter(service: TrackingService): Router {
  const router = Router();

  router.get("/merchants/:merchantId/delivery-stats", async (req, res) => {
    const { merchantId } = merchantParams.parse(req.params);
    const { days } = statsQuery.parse(req.query);
    const stats = await service.deliveryStats(merchantId, days);
    res.json({ merchantId, days, stats });
  });

  return router;
}
