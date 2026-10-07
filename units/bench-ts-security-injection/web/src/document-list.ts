import { escapeHtml } from './escape-html.js';
import { firstField, parseMetadataXml } from './xml-document.js';

export interface QueuedFile {
  readonly fileName: string;
  readonly xml: string;
}

/** Lists the metadata files queued for upload, by the title each file declares. */
export function renderQueue(list: HTMLElement, files: readonly QueuedFile[]): void {
  const items = files.map((file) => {
    const declared = firstField(parseMetadataXml(file.xml), 'title')?.textContent.trim();
    const title = declared === undefined || declared === '' ? file.fileName : declared;
    return `<li title="${escapeHtml(file.fileName)}">${escapeHtml(title)}</li>`;
  });
  list.innerHTML = items.join('');
}
