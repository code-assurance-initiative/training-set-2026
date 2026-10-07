import { beforeEach, describe, expect, it, vi } from "vitest";

const insertOne = vi.fn();
const close = vi.fn();
const connect = vi.fn();
const collection = vi.fn(() => ({ insertOne }));
const db = vi.fn(() => ({ collection }));
const constructed: unknown[] = [];

vi.mock("mongodb", () => ({
  MongoClient: class {
    constructor(...args: unknown[]) {
      constructed.push(args);
    }
    connect = connect;
    close = close;
    db = db;
  },
}));

const { LogOnlyAuditLog, MongoAuditLog } = await import("../../src/audit/auditLog.js");

const event = { action: "media.uploaded", mediaId: "m-1", actor: "studio-north", at: new Date("2026-03-01T12:00:00Z") } as const;

describe("MongoAuditLog", () => {
  beforeEach(() => {
    insertOne.mockReset().mockResolvedValue({ acknowledged: true });
    connect.mockReset().mockResolvedValue(undefined);
    close.mockReset().mockResolvedValue(undefined);
  });

  it("writes events to audit.media_events and closes the client", async () => {
    const log = await MongoAuditLog.connect("mongodb://audit.test:27017");

    await log.record(event);
    await log.close();

    expect(constructed.at(-1)).toEqual(["mongodb://audit.test:27017", { appName: "media-intake", retryWrites: true }]);
    expect(db).toHaveBeenCalledWith("audit");
    expect(collection).toHaveBeenCalledWith("media_events");
    expect(insertOne).toHaveBeenCalledWith(event);
    expect(close).toHaveBeenCalledOnce();
  });
});

describe("LogOnlyAuditLog", () => {
  it("hands every event to the writer", async () => {
    const written: unknown[] = [];
    const log = new LogOnlyAuditLog((e) => written.push(e));

    await log.record(event);
    await log.close();

    expect(written).toEqual([event]);
  });
});
