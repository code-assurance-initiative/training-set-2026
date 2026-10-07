import path from 'node:path';
import nunjucks from 'nunjucks';
import type { DocumentRecord } from './document-repository.js';

const viewsDirectory = path.join(import.meta.dirname, '..', '..', 'views');

/** Renders the HTML card the archive front end shows for a document in result lists. */
export class CardRenderer {
  private readonly environment = new nunjucks.Environment(
    new nunjucks.FileSystemLoader(viewsDirectory, { noCache: false }),
    { autoescape: true, throwOnUndefined: false },
  );

  render(document: DocumentRecord, publicBaseUrl: string): string {
    return this.environment.render('document-card.njk', {
      document,
      created: document.createdAt.toISOString().slice(0, 10),
      link: `${publicBaseUrl}/documents/${encodeURIComponent(document.id)}`,
    });
  }
}
