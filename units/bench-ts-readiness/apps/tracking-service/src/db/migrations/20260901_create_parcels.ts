import type { Knex } from "knex";

export async function up(knex: Knex): Promise<void> {
  await knex.schema.createTable("parcels", (table) => {
    table.uuid("id").primary();
    table.string("merchant_id", 64).notNullable();
    table.string("tracking_number", 32).notNullable().unique();
    table.string("carrier", 8).notNullable();
    table.string("destination_country", 2).notNullable();
    table.string("status", 24).notNullable();
    table.string("pickup_point_id", 32).nullable();
    table.timestamp("hold_until", { useTz: true }).nullable();
    table.timestamp("created_at", { useTz: true }).notNullable();
    table.timestamp("updated_at", { useTz: true }).notNullable();
    table.index(["merchant_id", "created_at"]);
    table.index(["status"]);
  });
}

export async function down(knex: Knex): Promise<void> {
  await knex.schema.dropTable("parcels");
}
