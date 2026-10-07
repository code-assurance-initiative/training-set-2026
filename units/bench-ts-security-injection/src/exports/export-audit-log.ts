import { appendFile, mkdir } from 'node:fs/promises';
import path from 'node:path';

export interface ExportAuditEntry {
  readonly caller: string;
  readonly documentId: string;
  readonly format: string;
  readonly fileName: string;
}

/**
 * The export audit trail records administrators read: one line per export, appended to a text file
 * that the log shipper forwards verbatim.
 */
export class ExportAuditLog {
  constructor(
    private readonly file: string,
    private readonly now: () => Date = () => new Date(),
  ) {}

  async record(entry: ExportAuditEntry): Promise<void> {
    const line = `${this.now().toISOString()} export caller=${entry.caller} document=${entry.documentId} format=${entry.format} file="${entry.fileName}"\n`;
    await mkdir(path.dirname(this.file), { recursive: true });
    await appendFile(this.file, line, 'utf8');
  }
}
