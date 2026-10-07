import type { UsageReporter } from "../billing/usageReporter.js";
import type { MediaRepository } from "../db/mediaRepository.js";
import type { SlackDigest } from "../notify/slackDigest.js";

const BYTES_PER_GB = 1024 ** 3;
const HOURS_PER_DAY = 24;

export interface NightlyDeps {
  readonly media: Pick<MediaRepository, "usageOn">;
  readonly usage: Pick<UsageReporter, "report">;
  readonly digest: Pick<SlackDigest, "post">;
}

/** Reports yesterday's stored gigabyte-hours per owner to billing and posts the upload digest. */
export async function runNightly(deps: NightlyDeps, day: Date): Promise<{ reported: number }> {
  const isoDay = day.toISOString().slice(0, 10);
  const owners = await deps.media.usageOn(isoDay);
  const reportAt = new Date(`${isoDay}T23:59:59Z`);

  for (const owner of owners) {
    await deps.usage.report({
      stripeCustomerId: owner.stripeCustomerId,
      gigabyteHours: (owner.storedBytes / BYTES_PER_GB) * HOURS_PER_DAY,
      at: reportAt,
    });
  }

  await deps.digest.post({
    day: isoDay,
    uploads: owners.reduce((sum, owner) => sum + owner.uploadsOnDay, 0),
    bytes: owners.reduce((sum, owner) => sum + owner.storedBytes, 0),
  });

  return { reported: owners.length };
}
