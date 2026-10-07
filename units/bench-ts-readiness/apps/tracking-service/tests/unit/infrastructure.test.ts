import { describe, expect, it } from "vitest";
import { createDatabase, databaseReachable } from "../../src/db/connection.js";
import { runMigrations } from "../../src/db/migrator.js";
import { createLogger } from "../../src/logger.js";
import { createTestDatabase } from "../support/database.js";
import { newDb } from "pg-mem";
import type { Knex } from "knex";

describe("database connection", () => {
  it("configures the PostgreSQL client without connecting", async () => {
    const db = createDatabase("postgres://tracking@db.test:5432/tracking", 4);

    const { config } = db.client as { config: Knex.Config };
    expect(config).toMatchObject({ client: "pg", pool: { min: 0, max: 4 } });
    await db.destroy();
  });

  it("reports whether the database answers", async () => {
    const db = await createTestDatabase();

    expect(await databaseReachable(db)).toBe(true);
    await db.destroy();
    expect(await databaseReachable(db)).toBe(false);
  });
});

describe("migration command", () => {
  const env = { DATABASE_URL: "postgres://owner@db.test:5432/tracking", LOG_LEVEL: "silent" };

  it("applies the migrations once and is a no-op afterwards", async () => {
    const memory = newDb();
    const open = () => memory.adapters.createKnex() as Knex;

    expect(await runMigrations(env, open)).toBe(true);
    expect(await runMigrations(env, open)).toBe(true);
    const check = open();
    expect(await check("knex_migrations").count({ n: "*" })).toEqual([{ n: 3 }]);
    await check.destroy();
  });

  it("reports failure when a migration cannot be applied", async () => {
    const memory = newDb();
    memory.public.none("create table parcels (id text primary key)");

    expect(await runMigrations(env, () => memory.adapters.createKnex() as Knex)).toBe(false);
  });
});

describe("logger", () => {
  it("names the process and honours the level", () => {
    const logger = createLogger("warn", "worker");

    expect(logger.level).toBe("warn");
    expect(logger.bindings()).toMatchObject({ name: "worker" });
  });
});
