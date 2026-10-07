import { randomBytes } from "node:crypto";

import type { FastifyInstance } from "fastify";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { buildApp } from "../../src/app.js";
import { LogOnlyAuditLog, type AuditEvent } from "../../src/audit/auditLog.js";
import { UploadSignatureVerifier } from "../../src/auth/uploadSignature.js";
import { UploadService } from "../../src/media/uploadService.js";
import { InMemoryMediaRepository } from "../support/inMemoryMediaRepository.js";
import { InMemoryObjectStore } from "../support/inMemoryObjectStore.js";

const BOUNDARY = "----media-intake-test-boundary";

function multipartBody(fileName: string, contentType: string, content: Buffer): Buffer {
  return Buffer.concat([
    Buffer.from(
      `--${BOUNDARY}\r\nContent-Disposition: form-data; name="file"; filename="${fileName}"\r\nContent-Type: ${contentType}\r\n\r\n`,
    ),
    content,
    Buffer.from(`\r\n--${BOUNDARY}--\r\n`),
  ]);
}

describe("upload routes", () => {
  let app: FastifyInstance;
  let store: InMemoryObjectStore;
  let signatures: UploadSignatureVerifier;
  let audit: AuditEvent[];
  const uploaded = vi.fn().mockResolvedValue(undefined);
  const sendMediaReady = vi.fn().mockResolvedValue(undefined);
  const requestThumbnails = vi.fn().mockResolvedValue("job-1");
  const createUpgradeIntent = vi.fn().mockResolvedValue("pi_1_client_value");

  function signed(ownerId: string, payload: Buffer): Record<string, string> {
    const timestamp = Math.floor(Date.now() / 1000);
    return {
      "x-owner-id": ownerId,
      "x-upload-timestamp": String(timestamp),
      "x-upload-signature": signatures.sign(ownerId, timestamp, UploadService.bodyDigest(payload)),
    };
  }

  beforeEach(async () => {
    store = new InMemoryObjectStore();
    signatures = new UploadSignatureVerifier(randomBytes(32).toString("hex"));
    audit = [];
    const auditLog = new LogOnlyAuditLog((event) => audit.push(event));
    app = await buildApp(
      {
        uploads: new UploadService(store, new InMemoryMediaRepository(), auditLog),
        signatures,
        thumbnailer: { request: requestThumbnails },
        events: { uploaded },
        email: { sendMediaReady },
        plans: { createUpgradeIntent },
        audit: auditLog,
        publicBaseUrl: "https://media.local",
      },
      { maxUploadBytes: 1024 * 1024 },
    );
  });

  afterEach(async () => {
    await app.close();
  });

  async function upload(content: Buffer, contentType = "image/png", headers: Record<string, string> = {}) {
    const timestamp = Math.floor(Date.now() / 1000);
    const signature = signatures.sign("studio-north", timestamp, UploadService.bodyDigest(content));
    return app.inject({
      method: "POST",
      url: "/v1/media",
      headers: {
        "content-type": `multipart/form-data; boundary=${BOUNDARY}`,
        "x-owner-id": "studio-north",
        "x-upload-timestamp": String(timestamp),
        "x-upload-signature": signature,
        ...headers,
      },
      payload: multipartBody("Poster.PNG", contentType, content),
    });
  }

  it("stores a signed upload and fans it out", async () => {
    const content = randomBytes(512);

    const response = await upload(content, "image/png", { "x-notify-email": "owner@media.local" });

    expect(response.statusCode).toBe(201);
    const body = response.json<{ id: string; sizeBytes: number }>();
    expect(body.sizeBytes).toBe(512);
    const [stored] = [...store.objects.values()];
    expect(stored?.body.equals(content)).toBe(true);
    expect(uploaded).toHaveBeenCalledOnce();
    expect(requestThumbnails).toHaveBeenCalledOnce();
    expect(sendMediaReady).toHaveBeenCalledWith(expect.objectContaining({ to: "owner@media.local", fileName: "Poster.PNG" }));
    expect(audit.map((event) => event.action)).toEqual(["media.uploaded"]);
  });

  it("rejects an upload whose signature does not cover the body", async () => {
    const timestamp = Math.floor(Date.now() / 1000);
    const signature = signatures.sign("studio-north", timestamp, UploadService.bodyDigest(Buffer.from("other")));

    const response = await app.inject({
      method: "POST",
      url: "/v1/media",
      headers: {
        "content-type": `multipart/form-data; boundary=${BOUNDARY}`,
        "x-owner-id": "studio-north",
        "x-upload-timestamp": String(timestamp),
        "x-upload-signature": signature,
      },
      payload: multipartBody("a.png", "image/png", Buffer.from("content")),
    });

    expect(response.statusCode).toBe(401);
    expect(store.objects.size).toBe(0);
  });

  it("refuses content types outside the allow-list", async () => {
    const response = await upload(Buffer.from("#!/bin/sh"), "application/x-sh");

    expect(response.statusCode).toBe(415);
  });

  it("serves the object for a valid download token and refuses it for another object", async () => {
    const content = randomBytes(64);
    const { id } = (await upload(content)).json<{ id: string }>();

    const tokenResponse = await app.inject({
      method: "POST",
      url: `/v1/media/${id}/download-token`,
      headers: signed("studio-north", Buffer.from(id)),
    });
    const { token } = tokenResponse.json<{ token: string }>();

    const download = await app.inject({ method: "GET", url: `/v1/media/${id}?token=${token}` });
    expect(download.statusCode).toBe(200);
    expect(download.rawPayload.equals(content)).toBe(true);
    expect(download.headers["x-content-type-options"]).toBe("nosniff");

    const elsewhere = await app.inject({ method: "GET", url: `/v1/media/00000000-0000-4000-8000-000000000000?token=${token}` });
    expect(elsewhere.statusCode).toBe(403);
  });

  it("does not issue a download token to another owner", async () => {
    const { id } = (await upload(randomBytes(16))).json<{ id: string }>();

    const response = await app.inject({
      method: "POST",
      url: `/v1/media/${id}/download-token`,
      headers: signed("studio-south", Buffer.from(id)),
    });

    expect(response.statusCode).toBe(404);
  });

  it("refuses an unsigned download-token request even with the right owner header", async () => {
    const { id } = (await upload(randomBytes(16))).json<{ id: string }>();

    const response = await app.inject({
      method: "POST",
      url: `/v1/media/${id}/download-token`,
      headers: { "x-owner-id": "studio-north" },
    });

    expect(response.statusCode).toBe(401);
  });

  it("creates a plan upgrade intent only for a signed request", async () => {
    const unsigned = await app.inject({
      method: "POST",
      url: "/v1/plans/upgrade-intent",
      headers: { "x-owner-id": "studio-north" },
    });
    const accepted = await app.inject({
      method: "POST",
      url: "/v1/plans/upgrade-intent",
      headers: signed("studio-north", Buffer.from("plan-upgrade")),
    });

    expect(unsigned.statusCode).toBe(401);
    expect(accepted.json()).toEqual({ clientSecret: "pi_1_client_value" });
    expect(createUpgradeIntent).toHaveBeenCalledWith("studio-north");
  });

  it("answers the health probe", async () => {
    const response = await app.inject({ method: "GET", url: "/health" });

    expect(response.json()).toEqual({ status: "ok" });
  });
});
