import type pg from "pg";
import { describe, expect, it, vi } from "vitest";

import { PostgresMediaRepository, type MediaRecord } from "../../src/db/mediaRepository.js";

function fakePool(rows: unknown[]): { pool: pg.Pool; query: ReturnType<typeof vi.fn> } {
  const query = vi.fn().mockResolvedValue({ rows });
  return { pool: { query } as unknown as pg.Pool, query };
}

const record: MediaRecord = {
  id: "0b6f5c1e-2f43-4a77-9b8e-5d0c7a1f2e39",
  ownerId: "studio-north",
  objectKey: "studio-north/2026-03-01/0b6f5c1e.png",
  contentType: "image/png",
  sizeBytes: 2048,
  createdAt: new Date("2026-03-01T12:00:00Z"),
};

describe("PostgresMediaRepository", () => {
  it("inserts with positional parameters only", async () => {
    const { pool, query } = fakePool([]);

    await new PostgresMediaRepository(pool).insert(record);

    expect(query).toHaveBeenCalledWith(expect.stringContaining("insert into media"), [
      record.id,
      record.ownerId,
      record.objectKey,
      record.contentType,
      record.sizeBytes,
      record.createdAt,
    ]);
  });

  it("maps a row back to a record and converts bigint text", async () => {
    const { pool } = fakePool([
      {
        id: record.id,
        owner_id: record.ownerId,
        object_key: record.objectKey,
        content_type: record.contentType,
        size_bytes: "2048",
        created_at: record.createdAt,
      },
    ]);

    await expect(new PostgresMediaRepository(pool).findById(record.id)).resolves.toEqual(record);
  });

  it("returns undefined when no row matches", async () => {
    const { pool } = fakePool([]);

    await expect(new PostgresMediaRepository(pool).findById("missing")).resolves.toBeUndefined();
  });

  it("reads daily usage per owner", async () => {
    const { pool, query } = fakePool([
      { owner_id: "studio-north", stripe_customer_id: "cus_A", stored_bytes: "1024", uploads_on_day: "2" },
    ]);

    const usage = await new PostgresMediaRepository(pool).usageOn("2026-03-01");

    expect(usage).toEqual([{ ownerId: "studio-north", stripeCustomerId: "cus_A", storedBytes: 1024, uploadsOnDay: 2 }]);
    expect(query).toHaveBeenCalledWith(expect.any(String), ["2026-03-01"]);
  });
});
