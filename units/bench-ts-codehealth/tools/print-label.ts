import { LabelArchive } from '../src/infrastructure/labels/label-archive.js';
import { LabelPrinter, type PrintResult } from '../src/infrastructure/printing/label-printer.js';
import { PrinterStatusProbe } from '../src/infrastructure/printing/printer-status-probe.js';
import type { Logger } from 'pino';

export interface PrintOptions {
  labelId: string;
  archiveDir: string;
  printer?: string;
}

const defaultPrinter = 'zebra-dock-1';

/** Prints an archived label on a CUPS queue (the default dock printer unless one is named). */
export async function printLabel(options: PrintOptions, logger: Logger): Promise<PrintResult> {
  const printerName = options.printer?.trim();
  const printer = printerName.length > 0 ? printerName : defaultPrinter;
  const zpl = await new LabelArchive(options.archiveDir, logger).load(options.labelId);
  if (zpl === undefined) {
    throw new Error(`No archived label ${options.labelId}`);
  }
  const status = await new PrinterStatusProbe().status(printer);
  if (!status.ready) {
    throw new Error(`Printer ${printer} is not ready: ${status.detail}`);
  }
  return new LabelPrinter().print(printer, zpl);
}
