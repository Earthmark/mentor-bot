import Discord from "discord.js";
import Websocket from "ws";

import { getOrCreateTicket } from "./ticket";
import { SubscriptionNotifier } from "./subs";
import { IncomingMessage } from "http";
import { WsServerConnection } from "./server_connection";

export class WsServer {
  #ws: Websocket.Server;
  #channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel;
  #notifier: SubscriptionNotifier;
  constructor(
    port: number | undefined,
    channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel,
    notifier: SubscriptionNotifier
  ) {
    this.#ws = new Websocket.Server({
      port,
    });
    this.#channel = channel;
    this.#notifier = notifier;

    this.#ws.on("connection", this.#onConnection);
  }

  #onConnection = (s: Websocket, msg: IncomingMessage): void => {
    if (!msg.url?.startsWith("/mentee?")) {
      s.close();
      return;
    }

    getOrCreateTicket(
      this.#channel,
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
          this.#channel,
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
