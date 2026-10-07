import type { Knex } from "knex";

export async function up(knex: Knex): Promise<void> {
  await knex.schema.createTable("tracking_events", (table) => {
    table.bigIncrements("id");
    table.uuid("parcel_id").notNullable().references("parcels.id").onDelete("CASCADE");
    table.string("carrier_code", 8).notNullable();
    table.string("status", 24).notNullable();
    table.timestamp("occurred_at", { useTz: true }).notNullable();
    table.timestamp("recorded_at", { useTz: true }).notNullable();
    table.unique(["parcel_id", "carrier_code", "occurred_at"]);
  });

  await knex.schema.createTable("merchant_webhooks", (table) => {
    table.string("merchant_id", 64).primary();
    table.string("url", 2048).notNullable();
    table.boolean("active").notNullable().defaultTo(true);
  });

  await knex.schema.createTable("webhook_outbox", (table) => {
    table.bigIncrements("id");
    table.string("merchant_id", 64).notNullable();
    table.uuid("parcel_id").notNullable().references("parcels.id").onDelete("CASCADE");
    table.string("event_type", 48).notNullable();
    table.jsonb("payload").notNullable();
    table.integer("attempts").notNullable().defaultTo(0);
    table.timestamp("created_at", { useTz: true }).notNullable();
    table.timestamp("next_attempt_at", { useTz: true }).notNullable();
    table.timestamp("delivered_at", { useTz: true }).nullable();
    table.index(["delivered_at", "next_attempt_at"]);
  });
}

export async function down(knex: Knex): Promise<void> {
  await knex.schema.dropTable("webhook_outbox");
  await knex.schema.dropTable("merchant_webhooks");
  await knex.schema.dropTable("tracking_events");
}
