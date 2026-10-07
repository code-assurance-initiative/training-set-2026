import { parseStringPromise } from 'xml2js';
import { z } from 'zod';
import type { StopStatus } from '../dispatch/dispatch-run.js';

export interface StatusEvent {
  readonly trackingNumber: string;
  readonly status: StopStatus;
  readonly at: string;
}

const carrierCodes: Readonly<Record<string, StopStatus>> = {
  OFD: 'out-for-delivery',
  DLV: 'delivered',
  NDL: 'failed',
};

const eventsSchema = z.object({
  event: z
    .array(
      z.object({
        $: z.object({
          tracking: z.string().regex(/^[A-Z0-9]{8,20}$/),
          code: z.string(),
          at: z.iso.datetime({ offset: true }),
        }),
      }),
    )
    .default([]),
});

/** xml2js reads an empty `<statusFeed/>` as an empty string. */
const feedSchema = z.object({
  statusFeed: z.union([z.literal('').transform(() => ({ event: [] })), eventsSchema]),
});

export class StatusFeedError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'StatusFeedError';
  }
}

/**
 * Reads the carrier's status feed (`<statusFeed><event tracking="…" code="DLV" at="…"/></statusFeed>`).
 * Codes this depot does not track (sorting-centre scans, customs) are skipped.
 */
export async function parseStatusFeed(xml: string): Promise<StatusEvent[]> {
  let document: unknown;
  try {
    document = await parseStringPromise(xml, { explicitRoot: true, explicitArray: true });
  } catch {
    throw new StatusFeedError('The status feed is not well-formed XML.');
  }
  const parsed = feedSchema.safeParse(document);
  if (!parsed.success) {
    throw new StatusFeedError('The status feed does not have the expected shape.');
  }
  return parsed.data.statusFeed.event.flatMap(({ $: event }) => {
    const status = carrierCodes[event.code];
    return status ? [{ trackingNumber: event.tracking, status, at: event.at }] : [];
  });
}
