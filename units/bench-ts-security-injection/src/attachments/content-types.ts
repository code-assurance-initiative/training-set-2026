import path from 'node:path';

const byExtension: Readonly<Record<string, string>> = {
  '.pdf': 'application/pdf',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.tif': 'image/tiff',
  '.tiff': 'image/tiff',
  '.txt': 'text/plain; charset=utf-8',
  '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  '.odt': 'application/vnd.oasis.opendocument.text',
};

/** The media type to serve a stored file as; anything unknown is served as opaque bytes. */
export function contentTypeOf(fileName: string): string {
  return byExtension[path.extname(fileName).toLowerCase()] ?? 'application/octet-stream';
}
