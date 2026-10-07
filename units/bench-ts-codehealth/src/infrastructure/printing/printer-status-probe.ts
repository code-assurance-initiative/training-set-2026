import { spawn } from 'node:child_process';
import { once } from 'node:events';

export interface PrinterStatus {
  readonly ready: boolean;
  readonly detail: string;
}

/** Asks CUPS (`lpstat -p`) whether a printer queue accepts jobs. */
export class PrinterStatusProbe {
  constructor(private readonly lpstatPath = 'lpstat') {}

  async status(printer: string): Promise<PrinterStatus> {
    const child = spawn(this.lpstatPath, ['-p', printer]);
    let output = '';
    let errors = '';
    child.stdout.setEncoding('utf8');
    child.stderr.setEncoding('utf8');
    child.stdout.on('data', (chunk: string) => {
      output += chunk;
    });
    child.stderr.on('data', (chunk: string) => {
      errors += chunk;
    });
    const [exitCode] = (await once(child, 'close')) as [number | null];
    if (exitCode !== 0) {
      return { ready: false, detail: errors.trim() || `lpstat exited with ${String(exitCode)}` };
    }
    return { ready: /is idle|now printing/.test(output), detail: output.trim() };
  }
}
