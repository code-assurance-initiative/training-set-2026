// @vitest-environment happy-dom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderQueue } from '../src/document-list.js';
import { renderPreview } from '../src/document-preview.js';
import { escapeHtml } from '../src/escape-html.js';
import { recentRecipients, rememberRecipients } from '../src/recent-recipients.js';
import { defaultViewSettings, loadViewSettings, saveViewSettings } from '../src/view-settings.js';
import { parseMetadataXml } from '../src/xml-document.js';

const metadata = (title: string) =>
  `<metadata><title>${title}</title><description>Scanned at station 4</description></metadata>`;

beforeEach(() => {
  document.body.innerHTML = '<div id="preview"></div><ul id="queue"></ul>';
  localStorage.clear();
});

function element(id: string): HTMLElement {
  const found = document.getElementById(id);
  if (!found) {
    throw new Error(`missing #${id}`);
  }
  return found;
}

describe('preview', () => {
  it('shows the title and description of a metadata file', () => {
    renderPreview(element('preview'), metadata('Board minutes'));

    expect(element('preview').querySelector('h3')?.textContent).toBe('Board minutes');
    expect(element('preview').querySelector('p')?.textContent).toBe('Scanned at station 4');
  });

  it('refuses a file that is not XML', () => {
    expect(() => {
      parseMetadataXml('<metadata><title>');
    }).toThrow(/well-formed/);
  });
});

describe('upload queue', () => {
  it('lists queued files by title, as text', () => {
    renderQueue(element('queue'), [
      { fileName: 'a.xml', xml: metadata('Minutes &lt;draft&gt;') },
      { fileName: 'b "final".xml', xml: '<metadata/>' },
    ]);

    const items = [...element('queue').querySelectorAll('li')];
    expect(items.map((item) => item.textContent)).toEqual(['Minutes <draft>', 'b "final".xml']);
    expect(items[1]?.getAttribute('title')).toBe('b "final".xml');
    expect(element('queue').querySelector('draft')).toBeNull();
  });

  it('escapes markup characters', () => {
    expect(escapeHtml(`<a href="x">'&'</a>`)).toBe(
      '&lt;a href=&quot;x&quot;&gt;&#39;&amp;&#39;&lt;/a&gt;',
    );
  });
});

describe('recent recipients', () => {
  it('remembers the latest ten addresses, newest first, without duplicates', () => {
    rememberRecipients(['a@example.org', 'b@example.org']);
    rememberRecipients(['c@example.org', 'a@example.org']);

    expect(recentRecipients()).toEqual(['c@example.org', 'a@example.org', 'b@example.org']);
    rememberRecipients(Array.from({ length: 12 }, (_, index) => `r${String(index)}@example.org`));
    expect(recentRecipients()).toHaveLength(10);
  });

  it('ignores unreadable storage and discards it', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    localStorage.setItem('archive.recentRecipients', '{not json');
    expect(recentRecipients()).toEqual([]);
    expect(localStorage.getItem('archive.recentRecipients')).toBeNull();
    expect(warn).toHaveBeenCalledOnce();
    warn.mockRestore();
    localStorage.setItem('archive.recentRecipients', '{"a":1}');
    expect(recentRecipients()).toEqual([]);
  });
});

describe('view settings', () => {
  it('round-trips the page size and theme', () => {
    saveViewSettings({ pageSize: 50, theme: 'dark' });

    expect(loadViewSettings()).toEqual({ pageSize: 50, theme: 'dark' });
  });

  it('falls back to the defaults for missing or invalid values', () => {
    expect(loadViewSettings()).toEqual(defaultViewSettings);
    localStorage.setItem('archive.viewSettings', '{"pageSize":7,"theme":"neon"}');
    expect(loadViewSettings()).toEqual(defaultViewSettings);
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    localStorage.setItem('archive.viewSettings', 'nope');
    expect(loadViewSettings()).toEqual(defaultViewSettings);
    expect(localStorage.getItem('archive.viewSettings')).toBeNull();
    warn.mockRestore();
  });
});
