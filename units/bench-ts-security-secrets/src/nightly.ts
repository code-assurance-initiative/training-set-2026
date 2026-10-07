import "dotenv/config";

import pino from "pino";

import { UsageReporter } from "./billing/usageReporter.js";
import { loadConfig } from "./config.js";
import { PostgresMediaRepository } from "./db/mediaRepository.js";
import { createPool } from "./db/pool.js";
import { runNightly } from "./jobs/nightly.js";
import { SlackDigest } from "./notify/slackDigest.js";

const config = loadConfig();
const log = pino({ name: "media-intake-nightly" });
const pool = createPool(config);
const yesterday = new Date(Date.now() - 24 * 60 * 60 * 1000);

try {
  const { reported } = await runNightly(
    {
      media: new PostgresMediaRepository(pool),
      usage: new UsageReporter(config),
      digest: new SlackDigest(config),
    },
    yesterday,
  );
  log.info({ reported }, "nightly usage reported");
} catch (error) {
  log.error({ err: error }, "nightly run failed");
  process.exitCode = 1;
} finally {
  await pool.end();
}
