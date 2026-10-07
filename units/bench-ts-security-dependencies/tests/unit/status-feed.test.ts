import { describe, expect, it } from 'vitest';
import { parseStatusFeed, StatusFeedError } from '../../src/carriers/status-feed.js';

describe('carrier status feed', () => {
  it('maps the codes this depot tracks and skips the others', async () => {
    const xml = `<?xml version="1.0"?>
      <statusFeed>
        <event tracking="AB12345678" code="OFD" at="2026-11-02T07:05:00+01:00"/>
        <event tracking="AB12345679" code="DLV" at="2026-11-02T09:41:00+01:00"/>
        <event tracking="AB12345680" code="NDL" at="2026-11-02T10:02:00+01:00"/>
        <event tracking="AB12345681" code="HUB" at="2026-11-02T03:12:00+01:00"/>
      </statusFeed>`;

    await expect(parseStatusFeed(xml)).resolves.toEqual([
      { trackingNumber: 'AB12345678', status: 'out-for-delivery', at: '2026-11-02T07:05:00+01:00' },
      { trackingNumber: 'AB12345679', status: 'delivered', at: '2026-11-02T09:41:00+01:00' },
      { trackingNumber: 'AB12345680', status: 'failed', at: '2026-11-02T10:02:00+01:00' },
    ]);
  });

  it('accepts an empty feed', async () => {
    await expect(parseStatusFeed('<statusFeed/>')).resolves.toEqual([]);
  });

  it('refuses XML that is not well-formed', async () => {
    await expect(parseStatusFeed('<statusFeed><event')).rejects.toThrow(/well-formed/);
  });

  it.each([
    '<deliveries/>',
    '<statusFeed><event tracking="ab" code="DLV" at="2026-11-02T09:41:00Z"/></statusFeed>',
    '<statusFeed><event tracking="AB12345679" code="DLV" at="yesterday"/></statusFeed>',
  ])('refuses a feed of the wrong shape: %s', async (xml) => {
    await expect(parseStatusFeed(xml)).rejects.toThrow(StatusFeedError);
  });
});
