import express from "express";
import http from "http";
import net from "net";

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

  app.get("/health", (_req, res) => {
    res.sendStatus(data.healthChecks.every((h) => h()) ? 200 : 500);
  });

  const server = app.listen(data.port);

  server.on("upgrade", data.wsHandler);

  return server;
};
