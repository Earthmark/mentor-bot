import express from "express";
import http from "http";
import ws from "ws";

import { logProm } from "./prom_catch";

// The http server is created here, routing websocket requests to a provided handler.
// This also does health check support through provided callbacks.

type HealthCallback = () => boolean;

const bindWsConnectionError = logProm("Error in websocket connection handler.");
const messageHandlerError = logProm("Error in message handler.");

const flagMapQuery = (query: URLSearchParams): Record<string, string> => {
  const q: Record<string, string> = {};
  query.forEach((val, key) => (q[key] = val));
  return q;
};

export type WsHandler = (
  ctor: Record<string, string | undefined>,
  accept: string | undefined,
  outboundHandler: (req: string) => void,
  close: () => void
) => Promise<{
  inboundHandler: (arg: string) => void;
  onClose: () => void;
}>;

const handleWs = (route: string, server: http.Server, handler: WsHandler) => {
  const wss = new ws.Server({
    host: `ws://www.host.com${route}`,
    noServer: true,
    server: server,
  });

  wss.on("connection", (ws, req): void =>
    bindWsConnectionError(async () => {
      const url = new URL("ws://localhost" + req.url);
      const query = flagMapQuery(url.searchParams);

      const { inboundHandler, onClose } = await handler(
        query,
        req.headers.accept,
        (outbound) => ws.send(outbound),
        () => ws.close()
      );

      ws.on("message", (msg) =>
        messageHandlerError(async () => {
          if (typeof msg === "string") {
            inboundHandler(msg);
          } else {
            throw new Error("Unsupported message format.");
          }
        })
      );

      ws.on("close", () => onClose());
    })
  );
};

export default (data: {
  port: number;
  healthChecks: HealthCallback[];
  menteeHandler: WsHandler;
  mentorHandler: WsHandler;
}): http.Server => {
  const app = express();

  // Health checks are always get at /health.
  app.get("/health", (_req, res) => {
    res.sendStatus(data.healthChecks.every((h) => h()) ? 200 : 500);
  });

  const server = app.listen(data.port);

  handleWs("/ws/mentee", server, data.menteeHandler);
  handleWs("/ws/mentor", server, data.mentorHandler);

  return server;
};
