import { describe, expect, it } from 'vitest';
import { CardRenderer } from '../../src/documents/card-renderer.js';
import { isDocumentNumber } from '../../src/documents/document-number.js';
import { DocumentRepository } from '../../src/documents/document-repository.js';
import { etagOf, matchesEtag } from '../../src/documents/etag.js';
import { sampleDocument } from '../support/documents.js';
import { FakeSqlClient } from '../support/fake-sql.js';

describe('document repository', () => {
  it('looks a document up by id and by number with bound parameters', async () => {
    const db = new FakeSqlClient().willReturn(sampleDocument).willReturn();
    const documents = new DocumentRepository(db);

    await expect(documents.getById(sampleDocument.id)).resolves.toBe(sampleDocument);
    await expect(documents.getByNumber('HR-2024-000404')).resolves.toBeUndefined();
    expect(db.queries.map((query) => query.values)).toEqual([
      [sampleDocument.id],
      ['HR-2024-000404'],
    ]);
  });

  it('reads the extracted text', async () => {
    const db = new FakeSqlClient().willReturn({ body: 'full text' });

    await expect(new DocumentRepository(db).getText(sampleDocument.id)).resolves.toBe('full text');
  });
});

describe('document numbers', () => {
  it.each(['HR-2024-001337', 'FIN-1999-1', 'ABCD-2026-999999'])('accepts %s', (value) => {
    expect(isDocumentNumber(value)).toBe(true);
  });

  it.each(['hr-2024-1', 'HR-24-1', 'HR-2024-', 'HR-2024-1234567', 'HR-2024-1 '])(
    'refuses %s',
    (value) => {
      expect(isDocumentNumber(value)).toBe(false);
    },
  );
});

describe('etag', () => {
  it('is stable for the same bytes and differs for others', () => {
    expect(etagOf('<p>a</p>')).toBe(etagOf('<p>a</p>'));
    expect(etagOf('<p>a</p>')).not.toBe(etagOf('<p>b</p>'));
    expect(etagOf('x')).toMatch(/^"[\w-]{22}"$/);
  });

  it('matches If-None-Match lists, weak validators and the wildcard', () => {
    const etag = etagOf('card');
    expect(matchesEtag(undefined, etag)).toBe(false);
    expect(matchesEtag(`"other", W/${etag}`, etag)).toBe(true);
    expect(matchesEtag('*', etag)).toBe(true);
    expect(matchesEtag('"other"', etag)).toBe(false);
  });
});

describe('card renderer', () => {
  it('escapes every value it renders', () => {
    const html = new CardRenderer().render(
      { ...sampleDocument, title: '<script>alert(1)</script>', collection: '"HR"' },
      'https://archive.test',
    );

    expect(html).toContain('&lt;script&gt;alert(1)&lt;/script&gt;');
    expect(html).toContain('&quot;HR&quot;');
    expect(html).toContain(`href="https://archive.test/documents/${sampleDocument.id}"`);
    expect(html).toContain('42 pages');
    expect(html).not.toContain('<script>');
  });

  it('omits the page count when it is unknown', () => {
    const html = new CardRenderer().render(
      { ...sampleDocument, pageCount: null },
      'https://archive.test',
    );

    expect(html).not.toContain('pages');
  });
});
