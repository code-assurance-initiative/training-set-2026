import type { AxiosAdapter, InternalAxiosRequestConfig } from "axios";
import { describe, expect, it } from "vitest";

import { TranscoderClient, createTranscoderHttp } from "../../src/transcode/transcoderClient.js";

describe("TranscoderClient", () => {
  it("posts a thumbnail job against the configured base URL", async () => {
    const seen: InternalAxiosRequestConfig[] = [];
    const adapter: AxiosAdapter = (config) => {
      seen.push(config);
      return Promise.resolve({ data: { jobId: "job-7", status: "queued" }, status: 201, statusText: "Created", headers: {}, config });
    };
    const http = createTranscoderHttp({ transcoderBaseUrl: "https://transcoder.test/v2" });
    http.defaults.adapter = adapter;

    const job = await new TranscoderClient(http).requestThumbnails("https://media.test/v1/media/m-1", "https://media.test/cb");

    expect(job).toEqual({ jobId: "job-7", status: "queued" });
    expect(seen[0]?.baseURL).toBe("https://transcoder.test/v2");
    expect(seen[0]?.url).toBe("/jobs");
    expect(JSON.parse(String(seen[0]?.data))).toMatchObject({ input: "https://media.test/v1/media/m-1" });
  });

  it("escapes the job id in the status path", async () => {
    const urls: (string | undefined)[] = [];
    const http = createTranscoderHttp({ transcoderBaseUrl: "https://transcoder.test/v2" });
    http.defaults.adapter = (config) => {
      urls.push(config.url);
      return Promise.resolve({ data: { jobId: "a/b", status: "done" }, status: 200, statusText: "OK", headers: {}, config });
    };

    await new TranscoderClient(http).jobStatus("a/b");

    expect(urls).toEqual(["/jobs/a%2Fb"]);
  });
});
