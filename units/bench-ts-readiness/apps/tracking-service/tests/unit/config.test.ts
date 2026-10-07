import { describe, expect, it } from "vitest";
import {
  ConfigError,
  loadApiConfig,
  loadMigrationConfig,
  loadWorkerConfig,
} from "../../src/config.js";

const database = { DATABASE_URL: "postgres://tracking@db.internal:5432/tracking" };

describe("configuration", () => {
  it("loads the API configuration with its defaults", () => {
    const config = loadApiConfig({
      ...database,
      PUBLIC_BASE_URL: "https://tracking.example.com",
      JWT_PUBLIC_KEY: "-----BEGIN PUBLIC KEY-----\nMFk…\n-----END PUBLIC KEY-----",
      JWT_ISSUER: "https://login.example.com/",
      JWT_AUDIENCE: "parcel-tracking",
      LINK_SIGNING_KEY: "x".repeat(32),
    });

    expect(config.PORT).toBe(8080);
    expect(config.DATABASE_POOL_MAX).toBe(10);
    expect(config.LOG_LEVEL).toBe("info");
  });

  it("loads the worker configuration and coerces numbers", () => {
    const config = loadWorkerConfig({
      ...database,
      CARRIER_API_URL: "https://carriers.example.com/",
      CARRIER_API_KEY: "k".repeat(16),
      WEBHOOK_SIGNING_SECRET: "w".repeat(32),
      POLL_INTERVAL_MS: "30000",
    });

    expect(config.POLL_INTERVAL_MS).toBe(30_000);
    expect(config.CARRIER_TIMEOUT_MS).toBe(5_000);
    expect(config.HEALTH_PORT).toBe(8081);
  });

  it("rejects an insecure carrier URL and names the variables, not their values", () => {
    const load = () =>
      loadWorkerConfig({
        ...database,
        CARRIER_API_URL: "http://carriers.example.com/",
        CARRIER_API_KEY: "short",
        WEBHOOK_SIGNING_SECRET: "w".repeat(32),
      });

    expect(load).toThrow(ConfigError);
    expect(load).toThrow(/CARRIER_API_URL/);
    expect(load).toThrow(/CARRIER_API_KEY/);
    expect(load).not.toThrow(/short/);
  });

  it("needs only the database for migrations", () => {
    expect(loadMigrationConfig(database).DATABASE_URL).toBe(database.DATABASE_URL);
    expect(() => loadMigrationConfig({ DATABASE_URL: "mysql://db/tracking" })).toThrow(ConfigError);
  });
});
