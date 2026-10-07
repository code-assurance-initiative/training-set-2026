import knex, { type Knex } from 'knex';
import pg from 'pg';

/** The PostgreSQL connection pool. */
export function createDatabase(connectionString: string): Knex {
  // A DATE is a calendar day: keep it as `YYYY-MM-DD` instead of a Date at local midnight.
  pg.types.setTypeParser(pg.types.builtins.DATE, (value: string) => value);
  return knex({
    client: 'pg',
    connection: connectionString,
    pool: { min: 0, max: 10 },
  });
}
