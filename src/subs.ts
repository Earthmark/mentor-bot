import Discord from 'discord.js';

type MessageSubscription = (msg: Discord.Message)=> void;

const subscriptions: {
  [key: string]: {
    counter: number,
    subs:{ [key: number]: MessageSubscription }
  } | undefined
} = {};

type SubscriptionToken = [string, number];

export function subscribeUpdates(id: Discord.Snowflake, subscriber: MessageSubscription): SubscriptionToken {
  let sub = subscriptions[id];
  if (!sub) {
    sub = subscriptions[id] = {
      counter: 0,
      subs: {}
    };
  }

  const counter = sub.counter;
  sub.counter++;
  sub.subs[counter] = subscriber;
  return [id, counter];
}

export function unsubscribeUpdates(id: SubscriptionToken){
  const sub = subscriptions[id[0]];
  if (sub) {
    const subs = sub.subs;
    delete subs[id[1]];
  }
}

export function invokeSubscriptions(message: Discord.Message): Discord.Message {
  const subs = subscriptions[message.id];
  if (subs) {
    Object.values(subs.subs).map(sub => sub(message));
  }
  return message;
}