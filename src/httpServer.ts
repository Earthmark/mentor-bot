import { App, Request } from "@tinyhttp/app";
import { tinyws, TinyWSRequest } from "tinyws";
import http from "http";
import { ParsedUrlQuery } from "querystring";

import { MenteeHandler, MenteeResponse } from "./mentee_ws_handler.js";
import { toObj } from "./req.js";
import { logProm } from "./prom_catch.js";
import { TicketGetOrCreateArgs } from "./ticket.js";

// The http server is created here, routing websocket requests to a provided handler.
// This also does health check support through provided callbacks.

type HealthCallback = () => boolean;

const bindHandlerError = logProm("Error in message handler.");
const messageHandlerError = logProm("Error in message handler.");

const flagMapQuery = (query: ParsedUrlQuery): Record<string, string> => {
  const q: Record<string, string> = {};
  Object.keys(query).forEach((key) => {
    const value = query[key];
    if (typeof value === "string") {
      q[key] = value;
    } else if (typeof value === "object") {
      q[key] = value[0];
    }
  }, {});
  return q;
};

export const createServer = (data: {
  port: number;
  healthChecks: HealthCallback[];
  menteeHandler: MenteeHandler;
}): http.Server => {
  const app = new App<Record<string, unknown>, Request & TinyWSRequest>();

  app.use(tinyws());

  // Health checks are always get at /health.
  app.get("/health", (_req, res) => {
    res.sendStatus(data.healthChecks.every((h) => h()) ? 200 : 500);
  });

  app.use("/mentee", (req, resp) =>
    bindHandlerError(async () => {
      if (req.ws) {
        const ws = await req.ws();

        const query = flagMapQuery(req.query);
        // TODO: Verify these arguments more aggressively.

        const { inboundHandler, onClose } = await data.menteeHandler(
          query as TicketGetOrCreateArgs,
          (outbound) => ws.send(outbound.toPayload(req.headers.accept)),
          () => ws.close()
        );

        ws.on("message", (msg) =>
          messageHandlerError(async () => {
            if (typeof msg === "string") {
              const obj = toObj(msg, req.headers.accept);
              inboundHandler(obj as MenteeResponse);
            } else {
              throw new Error("Unsupported message format.");
            }
          })
        );

        ws.on("close", () => onClose());
      } else {
        resp.sendStatus(400);
      }
    })
  );

  return app.listen(data.port);
};
