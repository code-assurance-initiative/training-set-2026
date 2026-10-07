import { Router } from "express";

export interface HealthChecks {
  ready: () => Promise<boolean>;
}

const securityTxt = [
  "Contact: https://github.com/code-assurance-initiative/bench-ts-readiness/security/advisories/new",
  "Expires: 2027-10-01T00:00:00.000Z",
  "Preferred-Languages: en, da",
  "Policy: https://github.com/code-assurance-initiative/bench-ts-readiness/blob/main/SECURITY.md",
  "",
].join("\n");

export function healthRouter(checks: HealthChecks): Router {
  const router = Router();

  router.get("/healthz", (_req, res) => {
    res.json({ status: "ok" });
  });

  router.get("/readyz", async (_req, res) => {
    const ready = await checks.ready();
    res.status(ready ? 200 : 503).json({ status: ready ? "ok" : "unavailable" });
  });

  router.get("/.well-known/security.txt", (_req, res) => {
    res.type("text/plain").send(securityTxt);
  });

  return router;
}
