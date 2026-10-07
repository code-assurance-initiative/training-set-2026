import { readFile } from 'node:fs/promises';
import path from 'node:path';

export class TemplateNotFoundError extends Error {
  constructor(name: string) {
    super(`No template named '${name}'.`);
    this.name = 'TemplateNotFoundError';
  }
}

/** Report templates kept as files under one directory. */
export class TemplateStore {
  private readonly root: string;

  constructor(root: string) {
    this.root = path.resolve(root);
  }

  /** The template's text. Names that resolve outside the template directory do not exist. */
  async read(name: string): Promise<string> {
    const resolved = path.resolve(this.root, name);
    if (!resolved.startsWith(this.root + path.sep)) {
      throw new TemplateNotFoundError(name);
    }
    try {
      return await readFile(resolved, 'utf8');
    } catch {
      throw new TemplateNotFoundError(name);
    }
  }
}
