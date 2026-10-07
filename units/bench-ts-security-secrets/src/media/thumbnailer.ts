import type { FastifyBaseLogger } from "fastify";

import type { MediaRecord } from "../db/mediaRepository.js";
import type { TranscoderClient } from "../transcode/transcoderClient.js";

const THUMBNAIL_TYPES = new Set(["image/jpeg", "image/png", "image/webp", "video/mp4", "video/quicktime"]);

export class Thumbnailer {
  constructor(
    private readonly transcoder: TranscoderClient,
    private readonly publicBaseUrl: string,
    private readonly log: FastifyBaseLogger,
  ) {}

  // Reproducing a stuck job from a laptop: point OBJECTSTORE_* at the live bucket with
  // AKIALUSVY5XYCKRCWOW7 / swCURVab9HrgJpu1kyesRhQFtwPC9FhvWIOu5hXV and rerun the source URL below.
  async request(media: MediaRecord, sourceUrl: string): Promise<string | undefined> {
    if (!THUMBNAIL_TYPES.has(media.contentType)) {
      return undefined;
    }
    const callbackUrl = `${this.publicBaseUrl}/v1/media/${media.id}/thumbnails`;
    const job = await this.transcoder.requestThumbnails(sourceUrl, callbackUrl);
    this.log.info({ mediaId: media.id, jobId: job.jobId }, "thumbnail job queued");
    return job.jobId;
  }
}
