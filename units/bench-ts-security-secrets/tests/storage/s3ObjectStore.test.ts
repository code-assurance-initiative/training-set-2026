import { GetObjectCommand, PutObjectCommand, type S3Client } from "@aws-sdk/client-s3";
import { describe, expect, it, vi } from "vitest";

import { S3ObjectStore } from "../../src/storage/objectStore.js";

function clientReturning(result: unknown): { client: S3Client; send: ReturnType<typeof vi.fn> } {
  const send = vi.fn().mockImplementation(() => (result instanceof Error ? Promise.reject(result) : Promise.resolve(result)));
  return { client: { send } as unknown as S3Client, send };
}

describe("S3ObjectStore", () => {
  it("puts the object into the configured bucket with its content type", async () => {
    const { client, send } = clientReturning({});

    await new S3ObjectStore(client, "media").put("o/1.png", Buffer.from("png"), "image/png");

    const command = send.mock.calls[0]?.[0] as PutObjectCommand;
    expect(command).toBeInstanceOf(PutObjectCommand);
    expect(command.input).toMatchObject({ Bucket: "media", Key: "o/1.png", ContentType: "image/png" });
  });

  it("reads the object body back as a buffer", async () => {
    const { client, send } = clientReturning({
      Body: { transformToByteArray: () => Promise.resolve(new Uint8Array([1, 2, 3])) },
    });

    const body = await new S3ObjectStore(client, "media").get("o/1.png");

    expect(body).toEqual(Buffer.from([1, 2, 3]));
    expect(send.mock.calls[0]?.[0]).toBeInstanceOf(GetObjectCommand);
  });

  it("returns undefined for a missing key and rethrows anything else", async () => {
    const missing = Object.assign(new Error("not found"), { name: "NoSuchKey" });
    const denied = Object.assign(new Error("denied"), { name: "AccessDenied" });

    await expect(new S3ObjectStore(clientReturning(missing).client, "media").get("x")).resolves.toBeUndefined();
    await expect(new S3ObjectStore(clientReturning({}).client, "media").get("x")).resolves.toBeUndefined();
    await expect(new S3ObjectStore(clientReturning(denied).client, "media").get("x")).rejects.toThrow("denied");
  });
});
