import { firstField, parseMetadataXml } from './xml-document.js';

/** Shows the title and description of a metadata file before it is uploaded. */
export function renderPreview(container: HTMLElement, xml: string): void {
  const metadata = parseMetadataXml(xml);
  const title = firstField(metadata, 'title')?.textContent.trim() ?? '';
  const description = firstField(metadata, 'description')?.textContent.trim() ?? '';
  container.innerHTML = `<article class="preview" data-title="${title}"><h3>${title}</h3><p>${description}</p></article>`;
}
