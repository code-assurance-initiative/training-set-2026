import { chmod, mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { LabelPrinter } from '../../src/infrastructure/printing/label-printer.js';
import { PrinterStatusProbe } from '../../src/infrastructure/printing/printer-status-probe.js';

async function script(body: string): Promise<string> {
  const path = join(await mkdtemp(join(tmpdir(), 'cups-')), 'tool');
  await writeFile(path, `#!/bin/sh\n${body}\n`);
  await chmod(path, 0o755);
  return path;
}

describe('CUPS integration', () => {
  // The fakes are POSIX shell scripts standing in for lp and lpstat; Windows has neither.
  it.skipIf(process.platform === 'win32')('prints raw ZPL and reports the job id', async () => {
    const lp = await script('cat > /dev/null; echo "request id is dock-1-42 (1 file(s))"');
    expect(await new LabelPrinter(lp).print('dock-1', '^XA^XZ')).toEqual({
      jobId: 'dock-1-42',
      exitCode: 0,
    });
  });

  it.skipIf(process.platform === 'win32')('reads the printer status from lpstat', async () => {
    const idle = await script('echo "printer dock-1 is idle.  enabled since today"');
    expect(await new PrinterStatusProbe(idle).status('dock-1')).toMatchObject({ ready: true });
    const missing = await script('echo "lpstat: Invalid destination name" >&2; exit 1');
    expect(await new PrinterStatusProbe(missing).status('dock-9')).toEqual({
      ready: false,
      detail: 'lpstat: Invalid destination name',
    });
  });
});
