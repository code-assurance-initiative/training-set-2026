/** A repeating timer that does not keep the process alive. Dispose it to stop it. */
export class RefreshTimer {
  readonly #handle: NodeJS.Timeout;

  constructor(intervalMs: number, tick: () => void) {
    this.#handle = setInterval(tick, intervalMs);
    this.#handle.unref();
  }

  dispose(): void {
    clearInterval(this.#handle);
  }
}
