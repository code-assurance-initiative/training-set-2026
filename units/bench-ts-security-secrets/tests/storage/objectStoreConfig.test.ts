import { readFile } from "node:fs/promises";

import { describe, expect, it } from "vitest";

import { objectStoreConfigFromEnv } from "../../src/storage/objectStore.js";

describe("objectStoreConfigFromEnv", () => {
  it("takes every value from the environment when it is set", async () => {
    const env = JSON.parse(await readFile(new URL("../fixtures/storage.json", import.meta.url), "utf8")) as Record<
      string,
      string
    >;

    const config = objectStoreConfigFromEnv(env);

    expect(config).toEqual({
      endpoint: env.OBJECTSTORE_ENDPOINT,
      region: env.OBJECTSTORE_REGION,
      bucket: env.OBJECTSTORE_BUCKET,
      accessKeyId: env.OBJECTSTORE_ACCESS_KEY_ID,
      secretAccessKey: env.OBJECTSTORE_SECRET_ACCESS_KEY,
    });
  });
});
