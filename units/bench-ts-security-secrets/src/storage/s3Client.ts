import { S3Client } from "@aws-sdk/client-s3";

import type { ObjectStoreConfig } from "./objectStore.js";

/**
 * Builds the S3 client for any S3-compatible store. Credentials have the AWS shape: an access key id such as
 * `AKIAIOSFODNN7EXAMPLE` and a 40-character secret access key such as `wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY`.
 * Path-style addressing is forced because most self-hosted stores do not serve virtual-host buckets.
 */
export function createS3Client(config: ObjectStoreConfig): S3Client {
  return new S3Client({
    endpoint: config.endpoint,
    region: config.region,
    forcePathStyle: true,
    credentials: {
      accessKeyId: config.accessKeyId,
      secretAccessKey: config.secretAccessKey,
    },
  });
}
