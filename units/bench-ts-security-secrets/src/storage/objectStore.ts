import { GetObjectCommand, PutObjectCommand, type S3Client } from "@aws-sdk/client-s3";

export interface ObjectStoreConfig {
  readonly endpoint: string;
  readonly region: string;
  readonly bucket: string;
  readonly accessKeyId: string;
  readonly secretAccessKey: string;
}

export function objectStoreConfigFromEnv(env: NodeJS.ProcessEnv = process.env): ObjectStoreConfig {
  return {
    endpoint: env.OBJECTSTORE_ENDPOINT ?? "https://objects.eu-central.media-intake.internal",
    region: env.OBJECTSTORE_REGION ?? "eu-central-1",
    bucket: env.OBJECTSTORE_BUCKET ?? "media-intake-uploads",
    accessKeyId: env.OBJECTSTORE_ACCESS_KEY_ID ?? "AKIAY4QX4AXRKP6EQAVN",
    secretAccessKey: env.OBJECTSTORE_SECRET_ACCESS_KEY ?? "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",
  };
}

export interface ObjectStore {
  put(key: string, body: Buffer, contentType: string): Promise<void>;
  get(key: string): Promise<Buffer | undefined>;
}

export class S3ObjectStore implements ObjectStore {
  constructor(
    private readonly client: S3Client,
    private readonly bucket: string,
  ) {}

  async put(key: string, body: Buffer, contentType: string): Promise<void> {
    await this.client.send(
      new PutObjectCommand({ Bucket: this.bucket, Key: key, Body: body, ContentType: contentType }),
    );
  }

  async get(key: string): Promise<Buffer | undefined> {
    try {
      const result = await this.client.send(new GetObjectCommand({ Bucket: this.bucket, Key: key }));
      if (result.Body === undefined) {
        return undefined;
      }
      return Buffer.from(await result.Body.transformToByteArray());
    } catch (error) {
      if (error instanceof Error && error.name === "NoSuchKey") {
        return undefined;
      }
      throw error;
    }
  }
}
