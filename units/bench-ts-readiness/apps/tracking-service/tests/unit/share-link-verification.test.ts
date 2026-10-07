import { randomBytes } from "node:crypto";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ShareLinks } from "../../src/links/share-links.js";

describe("share link expiry", () => {
  const issuedAt = new Date("2026-10-01T09:59:59.500Z");
  let links: ShareLinks;

  beforeEach(() => {
    vi.useFakeTimers({ now: issuedAt });
    links = new ShareLinks(randomBytes(32).toString("base64url"), "https://tracking.example.com/");
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  const signatureOf = (url: string) => new URL(url).searchParams.get("sig") ?? "";

  it("stamps the expiry from the clock at creation, in whole seconds", () => {
    const link = links.create("NPX12345678");

    expect(link.expiresAt).toBe(Date.parse("2026-10-01T10:14:59Z") / 1000);
  });

  it("accepts a link up to and including its expiry second", () => {
    const link = links.create("NPX12345678");

    vi.setSystemTime(new Date("2026-10-01T10:14:59.999Z"));

    expect(links.verify("NPX12345678", link.expiresAt, signatureOf(link.url))).toBe(true);
  });

  it("rejects a link from the second after its expiry", () => {
    const link = links.create("NPX12345678");

    vi.setSystemTime(new Date("2026-10-01T10:15:00.000Z"));

    expect(links.verify("NPX12345678", link.expiresAt, signatureOf(link.url))).toBe(false);
  });

  it("rejects an expiry that is not a whole number of seconds", () => {
    const link = links.create("NPX12345678");

    expect(links.verify("NPX12345678", link.expiresAt + 0.5, signatureOf(link.url))).toBe(false);
  });
});
