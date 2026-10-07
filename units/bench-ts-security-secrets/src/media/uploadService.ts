import { createHash, randomUUID } from "node:crypto";

import type { AuditLog } from "../audit/auditLog.js";
import type { MediaRecord, MediaRepository } from "../db/mediaRepository.js";
import type { ObjectStore } from "../storage/objectStore.js";

const ALLOWED_TYPES = new Set([
  "image/jpeg",
  "image/png",
  "image/webp",
  "video/mp4",
  "video/quicktime",
  "application/pdf",
]);

export class UnsupportedMediaTypeError extends Error {
  constructor(readonly contentType: string) {
    super(`content type ${contentType} is not accepted`);
    this.name = "UnsupportedMediaTypeError";
  }
}

export interface IncomingUpload {
  readonly ownerId: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly body: Buffer;
}

export class UploadService {
  constructor(
    private readonly store: ObjectStore,
    private readonly media: MediaRepository,
    private readonly audit: AuditLog,
    private readonly clock: () => Date = () => new Date(),
  ) {}

  static bodyDigest(body: Buffer): string {
    return createHash("sha256").update(body).digest("hex");
  }

  async accept(upload: IncomingUpload): Promise<MediaRecord> {
    if (!ALLOWED_TYPES.has(upload.contentType)) {
      throw new UnsupportedMediaTypeError(upload.contentType);
    }
    const id = randomUUID();
    const createdAt = this.clock();
    const objectKey = `${upload.ownerId}/${createdAt.toISOString().slice(0, 10)}/${id}${extensionOf(upload.fileName)}`;
    await this.store.put(objectKey, upload.body, upload.contentType);
    const record: MediaRecord = {
      id,
      ownerId: upload.ownerId,
      objectKey,
      contentType: upload.contentType,
      sizeBytes: upload.body.length,
      createdAt,
    };
    await this.media.insert(record);
    await this.audit.record({ action: "media.uploaded", mediaId: id, actor: upload.ownerId, at: createdAt });
    return record;
  }

  async read(mediaId: string): Promise<{ record: MediaRecord; body: Buffer } | undefined> {
    const record = await this.media.findById(mediaId);
    if (record === undefined) {
      return undefined;
    }
    const body = await this.store.get(record.objectKey);
    return body === undefined ? undefined : { record, body };
  }
}

function extensionOf(fileName: string): string {
  const match = /\.[A-Za-z0-9]{1,8}$/.exec(fileName);
  return match === null ? "" : match[0].toLowerCase();
}
