import type { ObjectStore } from "../../src/storage/objectStore.js";

export class InMemoryObjectStore implements ObjectStore {
  readonly objects = new Map<string, { body: Buffer; contentType: string }>();

  put(key: string, body: Buffer, contentType: string): Promise<void> {
    this.objects.set(key, { body, contentType });
    return Promise.resolve();
  }

  get(key: string): Promise<Buffer | undefined> {
    return Promise.resolve(this.objects.get(key)?.body);
  }
}
