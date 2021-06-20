import Discord from "discord.js";
import Websocket from "ws";

import { messageToString, setTicketCanceled, isFinal } from "./ticket";
import { SubscriptionNotifier, SubscriptionToken } from "./subs";
import { toObj } from "./req";
import { IncomingMessage } from "http";

export class WsServerConnection {
  #s: Websocket;
  #channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel;
  #notifier: SubscriptionNotifier;
  #accept: string | undefined;
  #sub: SubscriptionToken;
  #id: Discord.Snowflake;
  #ping: NodeJS.Timeout;

  constructor(
    s: Websocket,
    msg: IncomingMessage,
    ticket: Discord.Message,
    channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel,
    notifier: SubscriptionNotifier
  ) {
    this.#s = s;
    this.#notifier = notifier;
    this.#id = ticket.id;
    this.#channel = channel;
    this.#accept = msg.headers.accept;

    s.send(messageToString(ticket, this.#accept));

    s.on("message", this.#onMessage);

    this.#checkCanceled(ticket);

    this.#sub = this.#notifier.subscribe(ticket.id, this.#updateNotification);

    // Ping every 25 seconds so the server stays alive (in worst case).
    this.#ping = setInterval(() => s.ping(), 25000);

    s.on("close", this.#onClose);
  }

  #onClose = (): void => {
    this.#notifier.unsubscribe(this.#sub);
    clearInterval(this.#ping);
  };

  #updateNotification = (msg: Discord.Message): void => {
    this.#s.send(messageToString(msg, this.#accept));
    this.#checkCanceled(msg);
  };

  #onMessage = (d: Websocket.Data): void => {
    if (typeof d === "string") {
      const val = toObj(d, this.#accept);
      if (val.type === "cancel") {
        setTicketCanceled(this.#channel, this.#id)
          .then(this.#notifier.invoke)
          .catch((e) => console.log("Failed to cancel ticket", this.#id, e));
      }
    }
  };

  #checkCanceled = (message: Discord.Message): void => {
    if (isFinal(message)) {
      // Due to a bug in the neos cliet, send the message first, then cancel the websocket a bit later.
      // In this case 10 seconds.
      setTimeout(() => {
        this.#s.close();
      }, 10000);
    }
  };
}
