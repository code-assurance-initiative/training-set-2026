import type { Knex } from "knex";
import { newDb } from "pg-mem";
import { migrateToLatest } from "../../src/db/migrations/index.js";

/**
 * A throw-away in-memory PostgreSQL emulation with the service's real migrations applied, so every test runs
 * against the schema production gets.
 */
export async function createTestDatabase(): Promise<Knex> {
  const memory = newDb({ autoCreateForeignKeyIndices: true });
  const db = memory.adapters.createKnex() as Knex;
  await migrateToLatest(db);
  return db;
}

export async function registerWebhook(db: Knex, merchantId: string, url: string): Promise<void> {
  await db("merchant_webhooks").insert({ merchant_id: merchantId, url, active: true });
}
