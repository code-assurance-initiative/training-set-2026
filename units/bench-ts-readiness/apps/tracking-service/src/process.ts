import { once, type EventEmitter } from "node:events";
import type { Server } from "node:http";
import type { AddressInfo } from "node:net";
import { createDatabase, type Database } from "./db/connection.js";

/** What the process runners let a test replace: the database, and who hears the bound address. */
export interface ProcessHooks {
  openDatabase?: (url: string, poolMax: number) => Database;
  listening?: (address: AddressInfo) => void;
}

export const openDatabase = (
  config: { DATABASE_URL: string; DATABASE_POOL_MAX: number },
  hooks: ProcessHooks,
): Database =>
  (hooks.openDatabase ?? createDatabase)(config.DATABASE_URL, config.DATABASE_POOL_MAX);

/** Resolves on the first SIGTERM or SIGINT. */
export const stopRequested = (signals: EventEmitter): Promise<unknown> =>
  Promise.race([once(signals, "SIGTERM"), once(signals, "SIGINT")]);

/** Starts `server` on `port` (0: any free port) and reports the bound address. */
export async function listen(
  server: Server,
  port: number,
  hooks: ProcessHooks,
): Promise<AddressInfo> {
  server.listen(port);
  await once(server, "listening");
  const address = server.address() as AddressInfo;
  hooks.listening?.(address);
  return address;
}
