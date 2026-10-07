import { Router } from "express";
import { shareLinkQuery, trackingNumberParams } from "../schemas.js";
import { sendProblem } from "../errors.js";
import type { ShareLinks } from "../../links/share-links.js";
import type { TrackingService } from "../../parcels/tracking-service.js";

/** The recipient's tracking page data, reachable only through a signed, unexpired share link. */
export function publicTrackingRouter(service: TrackingService, links: ShareLinks): Router {
  const router = Router();

  router.get("/track/:trackingNumber", async (req, res) => {
    const { trackingNumber } = trackingNumberParams.parse(req.params);
    const { expires, sig } = shareLinkQuery.parse(req.query);
    const parcel = links.verify(trackingNumber, expires, sig)
      ? await service.publicStatus(trackingNumber)
      : undefined;
    if (!parcel) {
      sendProblem(res, 404, "Not found");
      return;
    }
    res.set("Cache-Control", "no-store").json({
      trackingNumber: parcel.trackingNumber,
      status: parcel.status,
      events: parcel.events.map((e) => ({
        status: e.status,
        occurredAt: e.occurredAt.toISOString(),
      })),
    });
  });

  return router;
}
