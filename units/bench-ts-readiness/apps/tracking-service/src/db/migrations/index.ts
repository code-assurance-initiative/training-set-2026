import type { Knex } from "knex";
import * as createParcels from "./20260901_create_parcels.js";
import * as createTrackingEvents from "./20260915_create_tracking_events.js";
import * as adoptCarriers from "./20261001_adopt_carriers.js";

interface Migration {
  name: string;
  module: Knex.Migration;
}

// Listed explicitly (in order) rather than discovered from the file system, so the compiled image, the tests and
// the specifications all apply exactly the same set.
const migrations: readonly Migration[] = [
  { name: "20260901_create_parcels", module: createParcels },
  { name: "20260915_create_tracking_events", module: createTrackingEvents },
  { name: "20261001_adopt_carriers", module: adoptCarriers },
];

export const migrationSource: Knex.MigrationSource<Migration> = {
  getMigrations: () => Promise.resolve([...migrations]),
  getMigrationName: (migration) => migration.name,
  getMigration: (migration) => Promise.resolve(migration.module),
};

/** Applies every pending migration; returns the names applied (empty when the schema is current). */
export async function migrateToLatest(db: Knex): Promise<string[]> {
  const [, applied] = (await db.migrate.latest({ migrationSource })) as [number, string[]];
  return applied;
}
