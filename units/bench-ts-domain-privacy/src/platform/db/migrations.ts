import type { Knex } from 'knex';
import * as membership from './migrations/001-membership.js';
import * as billing from './migrations/002-billing.js';
import * as auditAndOutbox from './migrations/003-audit-and-outbox.js';

interface Migration {
  up(db: Knex): Promise<void>;
  down(db: Knex): Promise<void>;
}

const migrations: ReadonlyMap<string, Migration> = new Map([
  ['001-membership', membership],
  ['002-billing', billing],
  ['003-audit-and-outbox', auditAndOutbox],
]);

const source: Knex.MigrationSource<string> = {
  getMigrations: () => Promise.resolve([...migrations.keys()]),
  getMigrationName: (name) => name,
  getMigration: (name) => {
    const migration = migrations.get(name);
    if (!migration) {
      throw new Error(`Unknown migration '${name}'.`);
    }
    return Promise.resolve(migration);
  },
};

/** Applies every migration that has not run yet; returns the names applied. */
export async function migrateToLatest(db: Knex): Promise<string[]> {
  const [, applied] = (await db.migrate.latest({ migrationSource: source })) as [number, string[]];
  return applied;
}
