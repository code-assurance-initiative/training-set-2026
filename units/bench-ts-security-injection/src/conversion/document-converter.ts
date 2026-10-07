import { exec } from 'node:child_process';
import path from 'node:path';
import { promisify } from 'node:util';
import type { ConversionSettings } from '../config.js';

const execAsync = promisify(exec);

export class ConversionFailedError extends Error {
  constructor(format: string, options: { cause: unknown }) {
    super(`Conversion to '${format}' failed.`, options);
    this.name = 'ConversionFailedError';
  }
}

/** Converts office documents with LibreOffice in headless mode. */
export class DocumentConverter {
  constructor(private readonly settings: ConversionSettings) {}

  /**
   * Converts `inputPath` into `outputDirectory` and returns the path of the result. `format` is a
   * LibreOffice filter specification such as `pdf` or `pdf:writer_pdf_Export`.
   */
  async convert(inputPath: string, format: string, outputDirectory: string): Promise<string> {
    const command = `${this.settings.sofficePath} --headless --convert-to ${format} --outdir ${outputDirectory} ${inputPath}`;
    try {
      await execAsync(command, { timeout: this.settings.timeoutMs });
    } catch (error) {
      throw new ConversionFailedError(format, { cause: error });
    }
    const extension = format.split(':')[0] ?? format;
    return path.join(outputDirectory, `${path.parse(inputPath).name}.${extension}`);
  }
}
