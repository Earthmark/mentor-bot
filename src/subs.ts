import Discord from "discord.js";

type MessageSubscription = (msg: Discord.Message) => void;

export type SubscriptionToken = [string, number];

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
    id: Discord.Snowflake,
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
    }
  };

  invoke = (message: Discord.Message): Discord.Message => {
    const subs = this.#subscriptions[message.id];
    if (subs) {
      Object.values(subs.subs).map((sub) => sub(message));
    }
    return message;
  };
}
