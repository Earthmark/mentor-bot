import Websocket from "ws";
import { IncomingMessage } from "http";

import { Ticket, TicketStore, TicketCreateArgs } from "./ticket";
import { SubscriptionNotifier, SubscriptionToken } from "./subs";
import { toObj } from "./req";

// This represents a websocket server clients can create tickets through.
export class WsServer {
  #ws: Websocket.Server;
  #store: TicketStore;
  #notifier: SubscriptionNotifier;
  #pingMs: number;
  #stopDelay: number;
  constructor(data: {
    port: number | undefined;
    store: TicketStore;
    notifier: SubscriptionNotifier;
    pingMs: number;
    stopDelay: number;
  }) {
    this.#ws = new Websocket.Server({
      port: data.port,
    });
    this.#store = data.store;
    this.#notifier = data.notifier;
    this.#pingMs = data.pingMs;
    this.#stopDelay = data.stopDelay;

    this.#ws.on("connection", this.#onConnection);
  }

  #onConnection = (s: Websocket, msg: IncomingMessage): void => {
    if (!msg.url?.startsWith("/mentee?")) {
      s.close();
      return;
    }

    const args = Object.fromEntries(
      new URL("ws://localhost" + msg.url).searchParams.entries()
    );

    this.#store
      .getOrCreateTicket(
        // this is bad, but we validate these later and the actual type is a bit funky.
        args as unknown as {
          ticket: string;
        } & TicketCreateArgs
      )
      .then((ticket) => {
        if (!ticket) {
          s.close();
          return;
        }

        return new WsServerConnection(
          s,
          msg,
          ticket,
          this.#store,
          this.#notifier,
          this.#pingMs,
          this.#stopDelay
        );
      })
      .catch((e) =>
        console.log(
          "Failed to bind observer for ticket, it likely didn't exist.",
          e
        )
      );
  };
}

// This represents a connection to a server, managing what format and how to notify back to the client.
export class WsServerConnection {
  #s: Websocket;
  #store: TicketStore;
  #notifier: SubscriptionNotifier;
  #accept: string | undefined;
  #sub: SubscriptionToken;
  #id: string;
  #ping: NodeJS.Timeout;
  #pingMs: number;
  #stopDelay: number;

  constructor(
    s: Websocket,
    msg: IncomingMessage,
    ticket: Ticket,
    store: TicketStore,
    notifier: SubscriptionNotifier,
    pingMs: number,
    stopDelay: number
  ) {
    this.#s = s;
    this.#notifier = notifier;
    this.#id = ticket.getId();
    this.#store = store;
    this.#accept = msg.headers.accept;
    this.#pingMs = pingMs;
    this.#stopDelay = stopDelay;

    this.#send(ticket);

    s.on("message", this.#onMessage);

    this.#checkCanceled(ticket);

    this.#sub = this.#notifier.subscribe(
      ticket.getId(),
      this.#updateNotification
    );

    // Ping every 25 seconds so the server stays alive (in worst case).
    this.#ping = setInterval(() => s.ping(), this.#pingMs);

    s.on("close", this.#onClose);
  }

  #send = (ticket: Ticket): void => {
    this.#s.send(ticket.toPayload(this.#accept));
  };

  #onClose = (): void => {
    this.#notifier.unsubscribe(this.#sub);
    clearInterval(this.#ping);
  };

  #updateNotification = (ticket: Ticket): void => {
    this.#send(ticket);
    this.#checkCanceled(ticket);
  };

  #onMessage = (d: Websocket.Data): void => {
    if (typeof d === "string") {
      const val = toObj(d, this.#accept);
      if (val.type === "cancel") {
        this.#store
          .getTicket(this.#id)
          .then((tick) => {
            if (!tick) {
              throw new Error("Ticket by requested id not found.");
            }
            return tick;
          })
          .then((ticket) => ticket.setCanceled())
          .then(this.#notifier.invoke)
          .catch((e) => console.log("Failed to cancel ticket", this.#id, e));
      }
    }
  };

  #checkCanceled = (ticket: Ticket): void => {
    if (ticket.isCompleted()) {
      // Due to a bug in the neos cliet, send the message first, then cancel the websocket a bit later.
      // In this case 10 seconds.
      setTimeout(() => {
        this.#s.close();
      }, this.#stopDelay);
    }
  };
}
