import { describe, expect, it } from "vitest";

import { loadConfig } from "../../src/config.js";
import { createPool } from "../../src/db/pool.js";

describe("createPool", () => {
  it("connects with DATABASE_URL when it is set", async () => {
    const url = "postgres://media@db.test:5432/media";
    const pool = createPool(loadConfig({ DATABASE_URL: url }));

    expect(pool.options.connectionString).toBe(url);
    expect(pool.options.application_name).toBe("media-intake");
    await pool.end();
  });
});
