import { randomBytes } from "node:crypto";
import { describe, expect, it } from "vitest";
import { ShareLinks, shareLinkLifetimeSeconds } from "../../src/links/share-links.js";

describe("share links", () => {
  const links = new ShareLinks(
    randomBytes(32).toString("base64url"),
    "https://tracking.example.com/",
  );

  it("expire fifteen minutes after they are created", () => {
    const link = links.create("NPX12345678");

    expect(link.expiresAt).toBe(Math.floor(Date.now() / 1000) + shareLinkLifetimeSeconds);
  });

  it("point at the public tracking page with the expiry and a signature", () => {
    const url = new URL(links.create("NPX12345678").url);

    expect(url.origin).toBe("https://tracking.example.com");
    expect(url.pathname).toBe("/track/NPX12345678");
    expect(url.searchParams.get("expires")).toMatch(/^\d+$/);
    expect(url.searchParams.get("sig")).toMatch(/^[\w-]{43}$/);
  });

  it("verify for the tracking number and expiry they were issued for", () => {
    const link = links.create("NPX12345678");
    const sig = new URL(link.url).searchParams.get("sig") ?? "";

    expect(links.verify("NPX12345678", link.expiresAt, sig)).toBe(true);
    expect(links.verify("NPX87654321", link.expiresAt, sig)).toBe(false);
    expect(links.verify("NPX12345678", link.expiresAt + 60, sig)).toBe(false);
  });

  it("do not verify with another key's signature", () => {
    const other = new ShareLinks(
      randomBytes(32).toString("base64url"),
      "https://tracking.example.com/",
    );
    const link = other.create("NPX12345678");
    const sig = new URL(link.url).searchParams.get("sig") ?? "";

    expect(links.verify("NPX12345678", link.expiresAt, sig)).toBe(false);
  });
});
