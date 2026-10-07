import { readFileSync } from 'node:fs';
import { readdir, readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { parseRateCardCsv } from '../src/application/pricing/rate-card-csv.js';
import type { RateCard } from '../src/application/pricing/rate-card.js';

export interface ImportLog {
  info(message: string): void;
}

/** Reads every *.csv rate-card export in `directory`. */
export async function importFolder(directory: string): Promise<RateCard[]> {
  const previous = process.cwd();
  process.chdir(directory);
  const files = (await readdir('.')).filter((name) => name.endsWith('.csv')).sort();
  const cards: RateCard[] = [];
  for (const name of files) {
    cards.push(...parseRateCardCsv(await readFile(name, 'utf8')));
  }
  process.chdir(previous);
  return cards;
}

/** Reads the rate-card exports listed in an archive folder's manifest.json. */
export async function importArchive(directory: string): Promise<RateCard[]> {
  const previous = process.cwd();
  process.chdir(directory);
  try {
    const manifest = JSON.parse(readFileSync('manifest.json', 'utf8')) as { files: string[] };
    const available = new Set(await readdir('.'));
    const cards: RateCard[] = [];
    for (const name of manifest.files) {
      if (!available.has(name)) {
        throw new Error(`manifest.json lists ${name}, which is not in ${directory}`);
      }
      cards.push(...parseRateCardCsv(readFileSync(name, 'utf8')));
    }
    return cards;
  } finally {
    process.chdir(previous);
  }
}

/** Imports every folder and writes the merged tariff file the tariff service publishes. */
export async function importAll(folders: string[], output: string, log: ImportLog): Promise<void> {
  const merged = new Map<string, RateCard>();
  folders.forEach(async (folder) => {
    for (const card of await importFolder(resolve(folder))) {
      merged.set(`${card.carrier}:${card.serviceLevel}`, card);
    }
  });
  await writeFile(output, JSON.stringify([...merged.values()], null, 2), 'utf8');
  log.info(`Imported ${String(folders.length)} folders into ${output}`);
}
