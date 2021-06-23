import { Ticket } from "./ticket";

type MessageSubscription = (msg: Ticket) => void;

export type SubscriptionToken = [string, number];

// A manager for ticket subscriptions, allowing the websocket service and maintainer to send notifications to each other.
// This may get replaced with a message buss if higher traffic loads are required (as this is currently single-instance).
export class SubscriptionNotifier {
  #subscriptions: {
    [key: string]:
      | {
          counter: number;
          subs: { [key: number]: MessageSubscription };
        }
      | undefined;
  } = {};

  subscribe = (
    id: string,
    subscriber: MessageSubscription
  ): SubscriptionToken => {
    let sub = this.#subscriptions[id];
    if (!sub) {
      sub = this.#subscriptions[id] = {
        counter: 0,
        subs: {},
      };
    }

    const counter = sub.counter;
    sub.counter++;
    sub.subs[counter] = subscriber;
    return [id, counter];
  };

  unsubscribe = (id: SubscriptionToken): void => {
    const sub = this.#subscriptions[id[0]];
    if (sub) {
      const subs = sub.subs;
      delete subs[id[1]];
      if (Object.keys(sub.subs).length === 0) {
        delete this.#subscriptions[id[0]];
      }
    }
  };

  invoke = (message: Ticket): Ticket => {
    const subs = this.#subscriptions[message.getId()];
    if (subs) {
      Object.values(subs.subs).forEach((sub) => sub(message));
    }
    return message;
  };
}
