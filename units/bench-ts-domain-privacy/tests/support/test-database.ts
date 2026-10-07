import type { Knex } from 'knex';
import { newDb } from 'pg-mem';
import { migrateToLatest } from '../../src/platform/db/migrations.js';

/** A fresh in-memory PostgreSQL emulation with every migration applied. */
export async function createTestDatabase(): Promise<Knex> {
  const db = newDb().adapters.createKnex() as Knex;
  await migrateToLatest(db);
  return db;
}
