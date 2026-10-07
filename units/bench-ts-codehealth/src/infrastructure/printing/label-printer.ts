import { spawn } from 'node:child_process';
import { once } from 'node:events';

export interface PrintResult {
  readonly jobId: string | undefined;
  readonly exitCode: number | null;
}

/** Sends ZPL to a CUPS printer queue with `lp` (raw mode). */
export class LabelPrinter {
  constructor(private readonly lpPath = 'lp') {}

  async print(printer: string, zpl: string): Promise<PrintResult> {
    const child = spawn(this.lpPath, ['-d', printer, '-o', 'raw']);
    let output = '';
    child.stdout.setEncoding('utf8');
    child.stdout.on('data', (chunk: string) => {
      output += chunk;
    });
    child.stdin.end(zpl);
    const [exitCode] = (await once(child, 'close')) as [number | null];
    return { jobId: /request id is (\S+)/.exec(output)?.[1], exitCode };
  }
}
