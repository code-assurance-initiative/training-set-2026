import { mkdtemp, readFile, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { importArchive, importFolder } from '../../tools/import-rate-cards.js';
import { run } from '../../tools/rates-cli.js';

const header =
  'carrier,service_level,currency,zone,per_kg,minimum,fuel_percent,volumetric_divisor,transit_days';

async function folderWith(files: Record<string, string>): Promise<string> {
  const directory = await mkdtemp(join(tmpdir(), 'rate-cards-'));
  for (const [name, content] of Object.entries(files)) {
    await writeFile(join(directory, name), content);
  }
  return directory;
}

describe('rate-card import', () => {
  it('reads every export in a folder and restores the working directory', async () => {
    const before = process.cwd();
    const folder = await folderWith({
      'alder.csv': `${header}\nalder,standard,DKK,Z1,1000,4900,5,5000,2`,
      'corvid.csv': `${header}\ncorvid,express,DKK,Z1,1400,6900,5,5000,1`,
      'notes.txt': 'ignored',
    });
    const cards = await importFolder(folder);
    expect(cards.map((card) => card.carrier)).toEqual(['alder', 'corvid']);
    expect(process.cwd()).toBe(before);
  });

  it('reads the exports a manifest lists, and refuses a missing one', async () => {
    const before = process.cwd();
    const folder = await folderWith({
      'manifest.json': JSON.stringify({ files: ['a.csv'] }),
      'a.csv': `${header}\nalder,economy,DKK,Z1,800,3900,5,5000,3`,
    });
    expect((await importArchive(folder))[0]?.serviceLevel).toBe('economy');
    await writeFile(join(folder, 'manifest.json'), JSON.stringify({ files: ['b.csv'] }));
    await expect(importArchive(folder)).rejects.toThrow(/b\.csv/);
    expect(process.cwd()).toBe(before);
  });

  it('runs the import command', async () => {
    const folder = await folderWith({
      'alder.csv': `${header}\nalder,standard,DKK,Z1,1000,4900,5,5000,2`,
    });
    const output = join(folder, 'tariff.json');
    expect(await run(['import', output, folder])).toBe(0);
    expect(JSON.parse(await readFile(output, 'utf8'))).toBeInstanceOf(Array);
  });

  it('prints usage for missing or unknown commands', async () => {
    expect(await run([])).toBe(0);
    expect(await run(['import'])).toBe(2);
    expect(await run(['print'])).toBe(2);
    expect(await run(['launch'])).toBe(2);
  });

  it('refuses to print a label that is not archived', async () => {
    const folder = await folderWith({});
    await expect(
      run(['print', 'label-1', '--printer', 'dock-1', '--archive', folder]),
    ).rejects.toThrow(/No archived label/);
  });
});
