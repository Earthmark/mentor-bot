import express from "express";
import http from "http";
import net from "net";

// The http server is created here, routing websocket requests to a provided handler.
// This also does health check support through provided callbacks.

type HealthCallback = () => boolean;

export const createServer = (data: {
  port: number;
  healthChecks: HealthCallback[];
  wsHandler: (
    request: http.IncomingMessage,
    socket: net.Socket,
    head: Buffer
  ) => void;
}): http.Server => {
  const app = express();

  // Health checks are always get at /health.
  app.get("/health", (_req, res) => {
    res.sendStatus(data.healthChecks.every((h) => h()) ? 200 : 500);
  });

  const server = app.listen(data.port);

  // Upgrade requests are websocket based.
  server.on("upgrade", data.wsHandler);

  return server;
};
