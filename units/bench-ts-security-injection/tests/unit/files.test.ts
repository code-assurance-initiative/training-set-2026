import { chmod, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { contentTypeOf } from '../../src/attachments/content-types.js';
import {
  ConversionFailedError,
  DocumentConverter,
} from '../../src/conversion/document-converter.js';
import { ThumbnailRenderer } from '../../src/conversion/thumbnail-renderer.js';
import { TemplateNotFoundError, TemplateStore } from '../../src/templates/template-store.js';
import { tempDirectory } from '../support/temp-dir.js';

let directory: { path: string; remove: () => Promise<void> };

beforeEach(async () => {
  directory = await tempDirectory();
});

afterEach(async () => {
  await directory.remove();
});

/** Writes an executable stand-in for an external tool. */
async function tool(name: string, script: string): Promise<string> {
  const file = path.join(directory.path, name);
  await writeFile(file, `#!/bin/sh\n${script}\n`);
  await chmod(file, 0o755);
  return file;
}

describe('template store', () => {
  beforeEach(async () => {
    await mkdir(path.join(directory.path, 'templates', 'letters'), { recursive: true });
    await writeFile(path.join(directory.path, 'templates', 'letters', 'cover.txt'), 'Dear reader');
    await writeFile(path.join(directory.path, 'secret.txt'), 'outside');
    await mkdir(path.join(directory.path, 'templates-old'));
    await writeFile(path.join(directory.path, 'templates-old', 'x.txt'), 'sibling');
  });

  it('reads a template inside its directory', async () => {
    const store = new TemplateStore(path.join(directory.path, 'templates'));

    await expect(store.read('letters/cover.txt')).resolves.toBe('Dear reader');
  });

  it.each(['../secret.txt', '../templates-old/x.txt', '/etc/hostname', '.', 'missing.txt'])(
    'refuses %s',
    async (name) => {
      const store = new TemplateStore(path.join(directory.path, 'templates'));

      await expect(store.read(name)).rejects.toBeInstanceOf(TemplateNotFoundError);
    },
  );
});

describe('document converter', () => {
  it('runs the converter and returns the converted file', async () => {
    const soffice = await tool(
      'soffice',
      'out="$5"; in="$6"; base=$(basename "$in"); echo converted > "$out/${base%.*}.pdf"',
    );
    const converter = new DocumentConverter({
      sofficePath: soffice,
      magickPath: '',
      timeoutMs: 5_000,
    });
    const input = path.join(directory.path, 'handbook.docx');
    await writeFile(input, 'original');

    const output = await converter.convert(input, 'pdf:writer_pdf_Export', directory.path);

    expect(output).toBe(path.join(directory.path, 'handbook.pdf'));
    await expect(readFile(output, 'utf8')).resolves.toBe('converted\n');
  });

  it('reports a failed conversion', async () => {
    const soffice = await tool('soffice', 'exit 3');
    const converter = new DocumentConverter({
      sofficePath: soffice,
      magickPath: '',
      timeoutMs: 5_000,
    });

    await expect(
      converter.convert('/nonexistent/in.docx', 'pdf', directory.path),
    ).rejects.toBeInstanceOf(ConversionFailedError);
  });
});

describe('thumbnail renderer', () => {
  it('passes the first page, the size and the output to the image tool', async () => {
    const magick = await tool('magick', 'printf "%s\\n" "$@" > "$(dirname "$0")/args.txt"');
    const renderer = new ThumbnailRenderer({
      sofficePath: '',
      magickPath: magick,
      timeoutMs: 5_000,
    });

    await renderer.render('/data/in file.pdf', '/tmp/out.png', 'small');

    const args = await readFile(path.join(directory.path, 'args.txt'), 'utf8');
    expect(args.trim().split('\n')).toEqual([
      '/data/in file.pdf[0]',
      '-thumbnail',
      '160x160',
      '-strip',
      'png:/tmp/out.png',
    ]);
  });
});

describe('content types', () => {
  it.each([
    ['scan.PDF', 'application/pdf'],
    ['photo.jpeg', 'image/jpeg'],
    ['notes.txt', 'text/plain; charset=utf-8'],
    ['archive.zip', 'application/octet-stream'],
  ])('serves %s as %s', (name, type) => {
    expect(contentTypeOf(name)).toBe(type);
  });
});
