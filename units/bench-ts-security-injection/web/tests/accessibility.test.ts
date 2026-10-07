// @vitest-environment happy-dom
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import axe from 'axe-core';
import { describe, expect, it } from 'vitest';
import { CardRenderer } from '../../src/documents/card-renderer.js';
import { renderQueue } from '../src/document-list.js';

const page = await readFile(path.join(import.meta.dirname, '..', 'index.html'), 'utf8');

async function violations(): Promise<string[]> {
  const results = await axe.run(document, { resultTypes: ['violations'] });
  return results.violations.map((violation) => `${violation.id}: ${violation.help}`);
}

describe('accessibility (axe)', () => {
  it('finds no violations on the upload page', async () => {
    document.documentElement.innerHTML = page.replace(
      /^<!doctype html>\s*<html lang="en">|<\/html>\s*$/gi,
      '',
    );
    document.documentElement.lang = 'en';
    const queue = document.getElementById('queue');
    if (queue) {
      renderQueue(queue, [
        { fileName: 'minutes.xml', xml: '<metadata><title>Minutes</title></metadata>' },
      ]);
    }

    expect(await violations()).toEqual([]);
  });

  it('finds no violations in a rendered document card', async () => {
    const card = new CardRenderer().render(
      {
        id: '2f1c0c5e-8a43-4d0e-9a51-0b8f5e6c7d21',
        number: 'HR-2024-001337',
        title: 'Staff handbook',
        collection: 'HR',
        pageCount: 42,
        createdAt: new Date('2024-03-01T09:30:00.000Z'),
        storagePath: 'hr/handbook.docx',
      },
      'https://archive.test',
    );
    document.documentElement.innerHTML = `<head><title>Card</title></head><body><main><h1>Archive</h1><section><h2>Results</h2>${card}</section></main></body>`;
    document.documentElement.lang = 'en';

    expect(await violations()).toEqual([]);
  });
});
