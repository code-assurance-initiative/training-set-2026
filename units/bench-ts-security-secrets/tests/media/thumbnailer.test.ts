import { describe, expect, it, vi } from "vitest";

import type { MediaRecord } from "../../src/db/mediaRepository.js";
import { Thumbnailer } from "../../src/media/thumbnailer.js";
import type { TranscoderClient } from "../../src/transcode/transcoderClient.js";
import { silentLogger } from "../support/silentLogger.js";

function media(contentType: string): MediaRecord {
  return {
    id: "m-1",
    ownerId: "studio-north",
    objectKey: "studio-north/2026-03-01/m-1",
    contentType,
    sizeBytes: 10,
    createdAt: new Date("2026-03-01T12:00:00Z"),
  };
}

describe("Thumbnailer", () => {
  it("queues a job with a callback under the public base URL", async () => {
    const requestThumbnails = vi.fn().mockResolvedValue({ jobId: "job-9", status: "queued" });
    const transcoder = { requestThumbnails } as unknown as TranscoderClient;

    const jobId = await new Thumbnailer(transcoder, "https://media.test", silentLogger).request(media("video/mp4"), "https://src");

    expect(jobId).toBe("job-9");
    expect(requestThumbnails).toHaveBeenCalledWith("https://src", "https://media.test/v1/media/m-1/thumbnails");
  });

  it("skips content the transcoder cannot thumbnail", async () => {
    const requestThumbnails = vi.fn();
    const transcoder = { requestThumbnails } as unknown as TranscoderClient;

    const jobId = await new Thumbnailer(transcoder, "https://media.test", silentLogger).request(media("application/pdf"), "https://src");

    expect(jobId).toBeUndefined();
    expect(requestThumbnails).not.toHaveBeenCalled();
  });
});
