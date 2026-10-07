import { createServer, type Server } from "node:http";

export interface WorkerHealth {
  live: () => boolean;
  ready: () => Promise<boolean>;
}

/** `/healthz` and `/readyz` for the kubelet; the worker serves nothing else. */
export function createHealthServer(health: WorkerHealth): Server {
  return createServer((req, res) => {
    const respond = (ok: boolean) => {
      res.writeHead(ok ? 200 : 503, { "content-type": "application/json" });
      res.end(JSON.stringify({ status: ok ? "ok" : "unavailable" }));
    };
    if (req.method !== "GET") {
      res.writeHead(405).end();
    } else if (req.url === "/healthz") {
      respond(health.live());
    } else if (req.url === "/readyz") {
      health.ready().then(respond, () => {
        respond(false);
      });
    } else {
      res.writeHead(404).end();
    }
  });
}
