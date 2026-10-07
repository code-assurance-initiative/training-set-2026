import knex, { type Knex } from "knex";

export type Database = Knex;

export function createDatabase(url: string, poolMax: number): Database {
  return knex({
    client: "pg",
    connection: { connectionString: url, application_name: "parcel-tracking" },
    pool: { min: 0, max: poolMax },
    acquireConnectionTimeout: 10_000,
  });
}

export async function databaseReachable(db: Database): Promise<boolean> {
  try {
    await db.raw("select 1");
    return true;
  } catch {
    return false;
  }
}
