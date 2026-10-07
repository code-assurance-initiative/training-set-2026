import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import type { ConversionSettings } from '../config.js';

const execFileAsync = promisify(execFile);

export const thumbnailSizes = ['small', 'medium', 'large'] as const;

export type ThumbnailSize = (typeof thumbnailSizes)[number];

const geometry: Readonly<Record<ThumbnailSize, string>> = {
  small: '160x160',
  medium: '320x320',
  large: '640x640',
};

/** Renders the first page of a stored document as a PNG thumbnail with ImageMagick. */
export class ThumbnailRenderer {
  constructor(private readonly settings: ConversionSettings) {}

  async render(inputPath: string, outputPath: string, size: ThumbnailSize): Promise<void> {
    await execFileAsync(
      this.settings.magickPath,
      [`${inputPath}[0]`, '-thumbnail', geometry[size], '-strip', `png:${outputPath}`],
      { timeout: this.settings.timeoutMs },
    );
  }
}
