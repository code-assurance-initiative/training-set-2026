import { XMLParser } from 'fast-xml-parser';

export interface FeedEntry {
  readonly id: string;
  readonly title: string;
  readonly updated: string;
  readonly link: string | null;
}

export class FeedNotAllowedError extends Error {
  constructor(host: string) {
    super(`Feeds are only read over https from partner hosts; '${host}' is not one.`);
    this.name = 'FeedNotAllowedError';
  }
}

export class FeedUnavailableError extends Error {
  constructor(status: number) {
    super(`The partner feed answered ${status}.`);
    this.name = 'FeedUnavailableError';
  }
}

interface AtomLink {
  readonly href?: string;
  readonly rel?: string;
}

interface AtomEntry {
  readonly id?: unknown;
  readonly title?: unknown;
  readonly updated?: unknown;
  readonly link?: AtomLink | AtomLink[];
}

const maximumEntries = 200;

function text(value: unknown): string {
  return typeof value === 'string' || typeof value === 'number' ? String(value) : '';
}

function alternateLink(link: AtomEntry['link']): string | null {
  const links = Array.isArray(link) ? link : link ? [link] : [];
  const alternate = links.find((candidate) => (candidate.rel ?? 'alternate') === 'alternate');
  return alternate?.href ?? null;
}

/** Reads the Atom feeds partner archives publish about documents transferred to us. */
export class PartnerFeedClient {
  private readonly allowedHosts: ReadonlySet<string>;

  constructor(
    allowedHosts: readonly string[],
    private readonly fetchImpl: typeof fetch = fetch,
  ) {
    this.allowedHosts = new Set(allowedHosts.map((host) => host.toLowerCase()));
  }

  async entries(feedUrl: string): Promise<FeedEntry[]> {
    const url = new URL(feedUrl);
    if (url.protocol !== 'https:' || !this.allowedHosts.has(url.hostname.toLowerCase())) {
      throw new FeedNotAllowedError(url.hostname);
    }
    const response = await this.fetchImpl(url, {
      redirect: 'error',
      signal: AbortSignal.timeout(10_000),
    });
    if (!response.ok) {
      throw new FeedUnavailableError(response.status);
    }
    const parser = new XMLParser({
      ignoreAttributes: false,
      attributeNamePrefix: '',
      isArray: (name) => name === 'entry',
    });
    const feed = parser.parse(await response.text()) as { feed?: { entry?: AtomEntry[] } };
    return (feed.feed?.entry ?? []).slice(0, maximumEntries).map((entry) => ({
      id: text(entry.id),
      title: text(entry.title),
      updated: text(entry.updated),
      link: alternateLink(entry.link),
    }));
  }
}
