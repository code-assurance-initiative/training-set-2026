import type { FastifyInstance, FastifyReply, FastifyRequest } from "fastify";

import type { AuditLog } from "../audit/auditLog.js";
import { issueDownloadToken, verifyDownloadToken } from "../auth/downloadTokens.js";
import { SignedOwner, type UploadSignatureVerifier } from "../auth/uploadSignature.js";
import type { EmailNotifier } from "../notify/email.js";
import type { UploadEvents } from "../notify/uploadEvents.js";
import type { Thumbnailer } from "./thumbnailer.js";
import { UnsupportedMediaTypeError, UploadService } from "./uploadService.js";

export interface UploadRouteDeps {
  readonly uploads: UploadService;
  readonly signatures: UploadSignatureVerifier;
  readonly thumbnailer: Pick<Thumbnailer, "request">;
  readonly events: Pick<UploadEvents, "uploaded">;
  readonly email: Pick<EmailNotifier, "sendMediaReady">;
  readonly audit: AuditLog;
  readonly publicBaseUrl: string;
}

type IdParams = { Params: { id: string } };
type DownloadRequest = { Params: { id: string }; Querystring: { token?: string } };

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function registerUploadRoutes(app: FastifyInstance, deps: UploadRouteDeps): void {
  app.post("/v1/media", (request, reply) => acceptUpload(deps, request, reply));
  app.post<IdParams>("/v1/media/:id/download-token", (request, reply) => issueToken(deps, request, reply));
  app.get<DownloadRequest>("/v1/media/:id", (request, reply) => download(deps, request, reply));
}

async function acceptUpload(deps: UploadRouteDeps, request: FastifyRequest, reply: FastifyReply): Promise<FastifyReply> {
  const notifyEmail = SignedOwner.header(request.headers["x-notify-email"]);
  if (notifyEmail !== undefined && !EMAIL.test(notifyEmail)) {
    return reply.code(400).send({ error: "x-notify-email is not an e-mail address" });
  }
  const file = await request.file();
  if (file === undefined) {
    return reply.code(400).send({ error: "a file part is required" });
  }
  const body = await file.toBuffer();
  const ownerId = SignedOwner.verify(deps.signatures, request.headers, UploadService.bodyDigest(body));
  if (ownerId === undefined) {
    return reply.code(401).send({ error: "request signature is missing, invalid or expired" });
  }

  try {
    const record = await deps.uploads.accept({ ownerId, fileName: file.filename, contentType: file.mimetype, body });
    const sourceToken = await issueDownloadToken({ mediaId: record.id, ownerId });
    const downloadUrl = `${deps.publicBaseUrl}/v1/media/${record.id}?token=${sourceToken}`;
    await deps.events.uploaded(record);
    await deps.thumbnailer.request(record, downloadUrl);
    if (notifyEmail !== undefined) {
      await deps.email.sendMediaReady({ to: notifyEmail, fileName: file.filename, downloadUrl });
    }
    return await reply.code(201).send({ id: record.id, sizeBytes: record.sizeBytes, contentType: record.contentType });
  } catch (error) {
    if (error instanceof UnsupportedMediaTypeError) {
      return reply.code(415).send({ error: error.message });
    }
    throw error;
  }
}

async function issueToken(
  deps: UploadRouteDeps,
  request: FastifyRequest<IdParams>,
  reply: FastifyReply,
): Promise<FastifyReply> {
  const mediaId = request.params.id;
  const ownerId = SignedOwner.verify(deps.signatures, request.headers, UploadService.bodyDigest(Buffer.from(mediaId)));
  if (ownerId === undefined) {
    return reply.code(401).send({ error: "request signature is missing, invalid or expired" });
  }
  const found = await deps.uploads.read(mediaId);
  if (found?.record.ownerId !== ownerId) {
    return reply.code(404).send({ error: "not found" });
  }
  const token = await issueDownloadToken({ mediaId, ownerId });
  await deps.audit.record({ action: "media.download-token-issued", mediaId, actor: ownerId, at: new Date() });
  return reply.send({ token, expiresInSeconds: 300 });
}

async function download(
  deps: UploadRouteDeps,
  request: FastifyRequest<DownloadRequest>,
  reply: FastifyReply,
): Promise<FastifyReply> {
  const token = request.query.token;
  if (token === undefined) {
    return reply.code(401).send({ error: "token is required" });
  }
  const grant = await verifyDownloadToken(token).catch(() => undefined);
  if (grant === undefined) {
    return reply.code(401).send({ error: "token is invalid or expired" });
  }
  if (grant.mediaId !== request.params.id) {
    return reply.code(403).send({ error: "token does not grant this object" });
  }
  const found = await deps.uploads.read(grant.mediaId);
  if (found === undefined) {
    return reply.code(404).send({ error: "not found" });
  }
  await deps.audit.record({ action: "media.downloaded", mediaId: grant.mediaId, actor: grant.ownerId, at: new Date() });
  return reply.type(found.record.contentType).send(found.body);
}
