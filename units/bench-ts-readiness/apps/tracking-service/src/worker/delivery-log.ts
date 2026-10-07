import type { Knex } from "knex";

export const deliveryLogTable = "webhook_delivery_attempts";

export interface DeliveryAttempt {
  outboxId: number;
  statusCode: number | null;
  error: string | null;
  attemptedAt: Date;
}

/** Makes sure the attempt log exists before the dispatcher writes to it. */
export async function ensureDeliveryLog(db: Knex): Promise<void> {
  if (await db.schema.hasTable(deliveryLogTable)) {
    return;
  }
  await db.schema.createTable(deliveryLogTable, (table) => {
    table.bigIncrements("id");
    table.bigInteger("outbox_id").notNullable().references("webhook_outbox.id").onDelete("CASCADE");
    table.integer("status_code").nullable();
    table.string("error", 500).nullable();
    table.timestamp("attempted_at", { useTz: true }).notNullable();
    table.index(["outbox_id"]);
  });
}

/** Every webhook delivery attempt, for merchant support. */
export class DeliveryLog {
  constructor(private readonly db: Knex) {}

  async record(attempt: DeliveryAttempt): Promise<void> {
    await this.db(deliveryLogTable).insert({
      outbox_id: attempt.outboxId,
      status_code: attempt.statusCode,
      error: attempt.error?.slice(0, 500) ?? null,
      attempted_at: attempt.attemptedAt,
    });
  }

  async attempts(outboxId: number): Promise<DeliveryAttempt[]> {
    const rows = await this.db(deliveryLogTable)
      .where({ outbox_id: outboxId })
      .orderBy("id")
      .select<
        {
          outbox_id: string | number;
          status_code: number | null;
          error: string | null;
          attempted_at: Date;
        }[]
      >("outbox_id", "status_code", "error", "attempted_at");
    return rows.map((r) => ({
      outboxId: Number(r.outbox_id),
      statusCode: r.status_code,
      error: r.error,
      attemptedAt: r.attempted_at,
    }));
  }
}
