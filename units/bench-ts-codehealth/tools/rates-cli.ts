import { pathToFileURL } from 'node:url';
import { pino } from 'pino';
import { importAll } from './import-rate-cards.js';
import { printLabel } from './print-label.js';

const usage = `Usage:
  parcel-rates import <output.json> <folder>...   merge rate-card exports into a tariff file
  parcel-rates print <label-id> [--printer <name>] [--archive <dir>]   print an archived label`;

/** Runs one CLI command; returns the process exit code. */
export async function run(argv: readonly string[]): Promise<number> {
  const logger = pino({ base: { tool: 'parcel-rates' } });
  const [command, ...rest] = argv;
  switch (command) {
    case 'import': {
      const [output, ...folders] = rest;
      if (!output || folders.length === 0) {
        process.stderr.write(`${usage}\n`);
        return 2;
      }
      await importAll(folders, output, { info: (message) => process.stdout.write(`${message}\n`) });
      return 0;
    }
    case 'print': {
      const [labelId] = rest;
      if (!labelId) {
        process.stderr.write(`${usage}\n`);
        return 2;
      }
      const result = await printLabel(
        {
          labelId,
          printer: option(rest, '--printer'),
          archiveDir: option(rest, '--archive') ?? './var/labels',
        },
        logger,
      );
      process.stdout.write(`Print job ${result.jobId ?? '(unknown)'}\n`);
      return result.exitCode === 0 ? 0 : 1;
    }
    default:
      process.stdout.write(`${usage}\n`);
      return command === undefined || command === 'help' ? 0 : 2;
  }
}

function option(args: readonly string[], name: string): string | undefined {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : undefined;
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
  process.exitCode = await run(process.argv.slice(2));
}
