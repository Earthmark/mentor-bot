import Websocket from "ws";
import http from "http";
import net from "net";

import { Ticket, TicketStore, TicketCreateArgs } from "./ticket";
import { SubscriptionNotifier } from "./subs";
import { toObj } from "./req";
import { logProm } from "./prom_catch";

// Websocket handling is defined here, but this is not the actual server.

const handlerError = logProm("Web request handler failed");

type WsHandler = (
  request: http.IncomingMessage,
  socket: net.Socket,
  head: Buffer
) => void;

// This represents a websocket server clients can create tickets through.
export const createWsServer = (data: {
  store: TicketStore;
  notifier: SubscriptionNotifier;
  pingMs: number;
  stopDelay: number;
}): WsHandler => {
  const wss = new Websocket.Server({
    noServer: true,
  });
  const store = data.store;
  const notifier = data.notifier;
  const pingMs = data.pingMs;
  const stopDelay = data.stopDelay;

  wss.on("connection", (s: Websocket, msg: http.IncomingMessage): void =>
    handlerError(async () => {
      const args = Object.fromEntries(
        new URL("ws://localhost" + msg.url).searchParams.entries()
      );

      const ticket = await store.getOrCreateTicket(
        // this is bad, but we validate these later and the actual type is a bit funky.
        args as unknown as {
          ticket: string;
        } & TicketCreateArgs
      );

      if (!ticket) {
        s.close();
        return;
      }

      bindConnection(s, msg, ticket, store, notifier, pingMs, stopDelay);
    })
  );

  return (request, socket, head) => {
    if (request.url?.startsWith("/mentee?")) {
      wss.handleUpgrade(request, socket, head, (ws, req) => {
        wss.emit("connection", ws, req);
      });
    } else {
      socket.destroy();
    }
  };
};

const ticketCancelFail = logProm("Failed to cancel ticket");

const bindConnection = (
  s: Websocket,
  msg: http.IncomingMessage,
  ticket: Ticket,
  store: TicketStore,
  notifier: SubscriptionNotifier,
  pingMs: number,
  stopDelay: number
) => {
  const id = ticket.getId();
  const accept = msg.headers.accept;

  const send = (ticket: Ticket): void => {
    s.send(ticket.toPayload(accept));
  };

  const checkCanceled = (ticket: Ticket): void => {
    if (ticket.isCompleted()) {
      // Due to a bug in the neos cliet, send the message first, then cancel the websocket a bit later.
      // In this case 10 seconds.
      setTimeout(() => {
        s.close();
      }, stopDelay);
    }
  };

  const updateNotification = (ticket: Ticket): void => {
    send(ticket);
    checkCanceled(ticket);
  };

  send(ticket);

  s.on("message", (d): void =>
    ticketCancelFail(async () => {
      if (typeof d === "string") {
        const val = toObj(d, accept);
        if (val.type === "cancel") {
          const ticket = await store.getTicket(id);
          if (!ticket) {
            throw new Error("Ticket by requested id not found.");
          }
          await ticket.setCanceled();
          notifier.invoke(ticket);
        }
      }
    })
  );

  checkCanceled(ticket);

  const sub = notifier.subscribe(ticket.getId(), updateNotification);

  // Ping every 25 seconds so the server stays alive (in worst case).
  const ping = setInterval(() => s.ping(), pingMs);

  s.on("close", (): void => {
    notifier.unsubscribe(sub);
    clearInterval(ping);
  });
};
