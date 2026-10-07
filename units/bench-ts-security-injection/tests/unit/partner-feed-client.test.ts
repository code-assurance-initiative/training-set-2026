import { describe, expect, it } from 'vitest';
import {
  FeedNotAllowedError,
  FeedUnavailableError,
  PartnerFeedClient,
} from '../../src/feeds/partner-feed-client.js';
import { fakeFetch } from '../support/fake-fetch.js';

const atom = `<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom">
  <title>Transfers</title>
  <entry>
    <id>urn:transfer:1</id>
    <title>Board minutes 1998</title>
    <updated>2026-09-30T10:00:00Z</updated>
    <link rel="alternate" href="https://feeds.partner-archive.test/t/1"/>
  </entry>
  <entry>
    <id>urn:transfer:2</id>
    <title>Maps</title>
    <updated>2026-09-29T10:00:00Z</updated>
  </entry>
</feed>`;

describe('partner feed client', () => {
  it('reads the entries of an allowed partner feed', async () => {
    const { fetchImpl, calls } = fakeFetch(new Response(atom, { status: 200 }));
    const client = new PartnerFeedClient(['feeds.partner-archive.test'], fetchImpl);

    const entries = await client.entries('https://FEEDS.partner-archive.test/transfers.atom');

    expect(entries).toEqual([
      {
        id: 'urn:transfer:1',
        title: 'Board minutes 1998',
        updated: '2026-09-30T10:00:00Z',
        link: 'https://feeds.partner-archive.test/t/1',
      },
      { id: 'urn:transfer:2', title: 'Maps', updated: '2026-09-29T10:00:00Z', link: null },
    ]);
    expect(calls[0]?.init?.redirect).toBe('error');
  });

  it.each([
    'http://feeds.partner-archive.test/transfers.atom',
    'https://feeds.partner-archive.test.attacker.test/x',
    'https://feeds.partner-archive.test@169.254.169.254/latest/meta-data',
    'https://localhost/admin',
  ])('refuses %s without a request', async (url) => {
    const { fetchImpl, calls } = fakeFetch();
    const client = new PartnerFeedClient(['feeds.partner-archive.test'], fetchImpl);

    await expect(client.entries(url)).rejects.toBeInstanceOf(FeedNotAllowedError);
    expect(calls).toHaveLength(0);
  });

  it('reports a failing partner', async () => {
    const { fetchImpl } = fakeFetch(new Response('', { status: 503 }));
    const client = new PartnerFeedClient(['feeds.partner-archive.test'], fetchImpl);

    await expect(client.entries('https://feeds.partner-archive.test/x')).rejects.toBeInstanceOf(
      FeedUnavailableError,
    );
  });
});
