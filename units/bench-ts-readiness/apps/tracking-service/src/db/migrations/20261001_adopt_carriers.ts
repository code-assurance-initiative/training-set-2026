import type { Knex } from "knex";

// The carriers table predates this service: the legacy dispatch database it now shares already has it in
// production, while new environments do not. Create it only where it is missing, then make sure the carriers this
// service polls are present.
export async function up(knex: Knex): Promise<void> {
  if (!(await knex.schema.hasTable("carriers"))) {
    await knex.schema.createTable("carriers", (table) => {
      table.string("code", 8).primary();
      table.string("name", 64).notNullable();
      table.string("tracking_url_template", 256).notNullable();
    });
  }

  await knex("carriers")
    .insert([
      {
        code: "NPX",
        name: "Nordpost Express",
        tracking_url_template: "https://track.nordpost.example/{tn}",
      },
      {
        code: "BLC",
        name: "Baltic Line Cargo",
        tracking_url_template: "https://blc.example/t/{tn}",
      },
    ])
    .onConflict("code")
    .ignore();
}

// Deliberately leaves the table in place: in adopted environments it belongs to the legacy system.
export async function down(): Promise<void> {
  // Nothing to undo that this migration owns exclusively.
}
