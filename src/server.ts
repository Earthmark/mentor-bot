import Websocket from "ws";
import { IncomingMessage } from "http";

import { Ticket, TicketStore } from "./ticket";
import { SubscriptionNotifier, SubscriptionToken } from "./subs";
import { toObj } from "./req";

export class WsServer {
  #ws: Websocket.Server;
  #store: TicketStore;
  #notifier: SubscriptionNotifier;
  constructor(
    port: number | undefined,
    store: TicketStore,
    notifier: SubscriptionNotifier
  ) {
    this.#ws = new Websocket.Server({
      port,
    });
    this.#store = store;
    this.#notifier = notifier;

    this.#ws.on("connection", this.#onConnection);
  }

  #onConnection = (s: Websocket, msg: IncomingMessage): void => {
    if (!msg.url?.startsWith("/mentee?")) {
      s.close();
      return;
    }

    this.#store
      .getOrCreateTicket(
        Object.fromEntries(
          new URL("ws://localhost" + msg.url).searchParams.entries()
        )
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
          this.#notifier
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

export class WsServerConnection {
  #s: Websocket;
  #store: TicketStore;
  #notifier: SubscriptionNotifier;
  #accept: string | undefined;
  #sub: SubscriptionToken;
  #id: string;
  #ping: NodeJS.Timeout;

  constructor(
    s: Websocket,
    msg: IncomingMessage,
    ticket: Ticket,
    store: TicketStore,
    notifier: SubscriptionNotifier
  ) {
    this.#s = s;
    this.#notifier = notifier;
    this.#id = ticket.getId();
    this.#store = store;
    this.#accept = msg.headers.accept;

    s.send(ticket.toPayload(this.#accept));

    s.on("message", this.#onMessage);

    this.#checkCanceled(ticket);

    this.#sub = this.#notifier.subscribe(
      ticket.getId(),
      this.#updateNotification
    );

    // Ping every 25 seconds so the server stays alive (in worst case).
    this.#ping = setInterval(() => s.ping(), 25000);

    s.on("close", this.#onClose);
  }

  #onClose = (): void => {
    this.#notifier.unsubscribe(this.#sub);
    clearInterval(this.#ping);
  };

  #updateNotification = (ticket: Ticket): void => {
    this.#s.send(ticket.toPayload(this.#accept));
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
      }, 10000);
    }
  };
}
