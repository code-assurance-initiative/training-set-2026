import { describe, expect, it } from "vitest";

import { createS3Client } from "../../src/storage/s3Client.js";

describe("createS3Client", () => {
  it("uses path-style addressing against the configured endpoint and region", async () => {
    const client = createS3Client({
      endpoint: "http://127.0.0.1:9000",
      region: "eu-central-1",
      bucket: "media",
      accessKeyId: "id-from-env",
      secretAccessKey: "value-from-env",
    });

    expect(client.config.forcePathStyle).toBe(true);
    await expect(client.config.region()).resolves.toBe("eu-central-1");
    const endpoint = await client.config.endpoint?.();
    expect(endpoint?.hostname).toBe("127.0.0.1");
    client.destroy();
  });
});
