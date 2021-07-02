type MessageSubscription<Notified> = (msg: Notified) => void;

export type SubscriptionToken = [string, number];

export type Notifier<Notified> = {
  invoke: (message: Notified) => Notified;
};

export type Subscriber<Notified> = {
  subscribe: (
    id: string,
    subscriber: MessageSubscription<Notified>
  ) => SubscriptionToken;
  unsubscribe: (id: SubscriptionToken) => void;
};

export default <
  Notified extends {
    id: string;
  }
>(): Notifier<Notified> & Subscriber<Notified> => {
  return new SubscriptionNotifier<Notified>();
};

// A manager for ticket subscriptions, allowing the websocket service and maintainer to send notifications to each other.
// This may get replaced with a message buss if higher traffic loads are required (as this is currently single-instance).
class SubscriptionNotifier<
  Notified extends {
    id: string;
  }
> {
  #subscriptions: {
    [key: string]:
      | {
          counter: number;
          subs: { [key: number]: MessageSubscription<Notified> };
        }
      | undefined;
  } = {};

  subscribe = (
    id: string,
    subscriber: MessageSubscription<Notified>
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

  invoke = (message: Notified): Notified => {
    const subs = this.#subscriptions[message.id];
    if (subs) {
      Object.values(subs.subs).forEach((sub) => sub(message));
    }
    return message;
  };
}
