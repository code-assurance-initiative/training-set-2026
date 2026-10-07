import type { FastifyInstance } from "fastify";

import { SignedOwner, type UploadSignatureVerifier } from "../auth/uploadSignature.js";
import { UploadService } from "../media/uploadService.js";

const STRIPE_API = "https://api.stripe.com/v1";
const TERABYTE_PLAN_AMOUNT_CENTS = 1900;
const OUTBOUND_TIMEOUT_MS = 10_000;

export class PlanUpgrades {
  constructor(
    private readonly apiKey: string,
    private readonly fetchImpl: typeof fetch = fetch,
  ) {}

  /** Creates the payment intent the browser confirms with Stripe.js; returns its client secret. */
  async createUpgradeIntent(ownerId: string): Promise<string> {
    const response = await this.fetchImpl(`${STRIPE_API}/payment_intents`, {
      method: "POST",
      signal: AbortSignal.timeout(OUTBOUND_TIMEOUT_MS),
      headers: {
        Authorization: `Bearer ${this.apiKey}`,
        "Content-Type": "application/x-www-form-urlencoded",
        "Idempotency-Key": `upgrade-${ownerId}-${new Date().toISOString().slice(0, 10)}`,
      },
      body: new URLSearchParams({
        amount: String(TERABYTE_PLAN_AMOUNT_CENTS),
        currency: "eur",
        "automatic_payment_methods[enabled]": "true",
        "metadata[owner_id]": ownerId,
        "metadata[plan]": "storage-1tb",
      }),
    });
    const intent = (await response.json()) as { client_secret?: string; error?: { message?: string } };
    if (!response.ok || intent.client_secret === undefined) {
      throw new Error(`payment intent was not created: ${intent.error?.message ?? `HTTP ${response.status}`}`);
    }
    return intent.client_secret;
  }
}

export const PLAN_UPGRADE_PURPOSE = "plan-upgrade";

export function registerPlanRoutes(
  app: FastifyInstance,
  plans: Pick<PlanUpgrades, "createUpgradeIntent">,
  signatures: UploadSignatureVerifier,
): void {
  app.post("/v1/plans/upgrade-intent", async (request, reply) => {
    const digest = UploadService.bodyDigest(Buffer.from(PLAN_UPGRADE_PURPOSE));
    const ownerId = SignedOwner.verify(signatures, request.headers, digest);
    if (ownerId === undefined) {
      return reply.code(401).send({ error: "request signature is missing, invalid or expired" });
    }
    return reply.send({ clientSecret: await plans.createUpgradeIntent(ownerId) });
  });
}
