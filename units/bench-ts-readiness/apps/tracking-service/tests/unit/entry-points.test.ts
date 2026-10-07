import { afterEach, describe, expect, it, vi } from "vitest";
import { ConfigError } from "../../src/config.js";

describe("entry points", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  // Each entry point is one call into a tested run function; with an invalid environment it must refuse to start.
  it.each(["../../src/api/main.js", "../../src/worker/main.js", "../../src/db/migrate.js"])(
    "%s refuses to start without a valid configuration",
    async (entry) => {
      vi.stubEnv("DATABASE_URL", "not a database url");

      await expect(import(entry)).rejects.toBeInstanceOf(ConfigError);
    },
  );
});
