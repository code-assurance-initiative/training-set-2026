import type { ErrorRequestHandler, Response } from "express";
import type { Logger } from "pino";
import { ZodError } from "zod";
import { DuplicateTrackingNumberError } from "../parcels/parcel-store.js";
import { ParcelNotFoundError, RedirectNotAllowedError } from "../parcels/tracking-service.js";

export function sendProblem(res: Response, status: number, title: string, detail?: string): void {
  res
    .status(status)
    .type("application/problem+json")
    .json(detail === undefined ? { title, status } : { title, status, detail });
}

export function errorHandler(logger: Logger): ErrorRequestHandler {
  return (error: unknown, _req, res, _next) => {
    if (error instanceof ZodError) {
      sendProblem(
        res,
        400,
        "Invalid request",
        error.issues.map((i) => `${i.path.join(".")}: ${i.message}`).join("; "),
      );
    } else if (error instanceof ParcelNotFoundError) {
      sendProblem(res, 404, "Not found", error.message);
    } else if (error instanceof DuplicateTrackingNumberError) {
      sendProblem(res, 409, "Conflict", error.message);
    } else if (error instanceof RedirectNotAllowedError) {
      sendProblem(res, 409, "Conflict", error.message);
    } else if (error instanceof SyntaxError && "body" in error) {
      sendProblem(res, 400, "Invalid request", "The body is not valid JSON");
    } else {
      logger.error({ err: error }, "unhandled request error");
      sendProblem(res, 500, "Internal server error");
    }
  };
}
