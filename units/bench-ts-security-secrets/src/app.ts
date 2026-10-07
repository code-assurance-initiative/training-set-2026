import multipart from "@fastify/multipart";
import Fastify, { type FastifyInstance, type FastifyServerOptions } from "fastify";

import { registerPlanRoutes, type PlanUpgrades } from "./billing/planUpgrades.js";
import { registerUploadRoutes, type UploadRouteDeps } from "./media/uploadRoutes.js";

export interface AppDeps extends UploadRouteDeps {
  readonly plans: Pick<PlanUpgrades, "createUpgradeIntent">;
}

export interface AppOptions {
  readonly maxUploadBytes: number;
  readonly logger?: FastifyServerOptions["logger"];
}

export async function buildApp(deps: AppDeps, options: AppOptions): Promise<FastifyInstance> {
  const app = Fastify({ logger: options.logger ?? false, bodyLimit: 1024 * 1024 });

  app.addHook("onSend", async (_request, reply, payload) => {
    reply.header("X-Content-Type-Options", "nosniff");
    reply.header("X-Frame-Options", "DENY");
    reply.header("Referrer-Policy", "no-referrer");
    reply.header("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
    reply.header("Strict-Transport-Security", "max-age=63072000; includeSubDomains");
    return payload;
  });

  await app.register(multipart, { limits: { fileSize: options.maxUploadBytes, files: 1 } });

  app.get("/health", () => ({ status: "ok" }));
  registerUploadRoutes(app, deps);
  registerPlanRoutes(app, deps.plans, deps.signatures);

  return app;
}
