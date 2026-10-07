import { Router } from "express";
import { createParcelBody, trackingNumberParams, type RedirectRequest } from "../schemas.js";
import { Scopes, principalOf, requireScope } from "../auth.js";
import type { ShareLinks } from "../../links/share-links.js";
import type { ParcelWithEvents, TrackingService } from "../../parcels/tracking-service.js";

const toResponse = (parcel: ParcelWithEvents) => ({
  trackingNumber: parcel.trackingNumber,
  carrier: parcel.carrier,
  destinationCountry: parcel.destinationCountry,
  status: parcel.status,
  pickupPointId: parcel.pickupPointId,
  holdUntil: parcel.holdUntil?.toISOString() ?? null,
  createdAt: parcel.createdAt.toISOString(),
  events: parcel.events.map((e) => ({
    code: e.carrierCode,
    status: e.status,
    occurredAt: e.occurredAt.toISOString(),
  })),
});

/** Merchant parcel routes; mounted behind authenticate(). */
export function parcelsRouter(service: TrackingService, links: ShareLinks): Router {
  const router = Router();

  router.post("/parcels", requireScope(Scopes.writeParcels), async (req, res) => {
    const body = createParcelBody.parse(req.body);
    const parcel = await service.register({ ...body, merchantId: principalOf(res).merchantId });
    res
      .status(201)
      .location(`/v1/parcels/${parcel.trackingNumber}`)
      .json(toResponse({ ...parcel, events: [] }));
  });

  router.get("/parcels/:trackingNumber", requireScope(Scopes.readParcels), async (req, res) => {
    const { trackingNumber } = trackingNumberParams.parse(req.params);
    res.json(toResponse(await service.get(principalOf(res).merchantId, trackingNumber)));
  });

  router.post(
    "/parcels/:trackingNumber/share-link",
    requireScope(Scopes.readParcels),
    async (req, res) => {
      const { trackingNumber } = trackingNumberParams.parse(req.params);
      await service.get(principalOf(res).merchantId, trackingNumber);
      res.status(201).json(links.create(trackingNumber));
    },
  );

  router.post(
    "/parcels/:trackingNumber/redirect",
    requireScope(Scopes.writeParcels),
    async (req, res) => {
      const { trackingNumber } = trackingNumberParams.parse(req.params);
      const body = req.body as RedirectRequest;
      await service.redirect(principalOf(res).merchantId, trackingNumber, {
        pickupPointId: body.pickupPointId,
        holdUntil: new Date(body.holdUntil),
      });
      res.status(204).end();
    },
  );

  return router;
}
