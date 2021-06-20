import Discord from "discord.js";

import {
  requested,
  responding,
  claimEmoji,
  unclaimEmoji,
  completeEmoji,
  getMentor,
  isFinal,
  setTicketResponding,
  setTicketCompleted,
  setTicketRequested,
} from "./ticket";
import { SubscriptionNotifier } from "./subs";
import { log } from "./prom_catch";

const processRootErr = log("Error while managing root requests");
const processRequestedErr = log("Error while processing a requested ticket");
const processRespondingErr = log("Error while replying to a responding ticket");

export class DiscordMaintainer {
  #channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel;
  #notifier: SubscriptionNotifier;
  constructor(
    channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel,
    notifer: SubscriptionNotifier
  ) {
    this.#channel = channel;
    this.#notifier = notifer;
  }

  start = async (): Promise<void> => {
    this.#channel
      .createMessageCollector((msg) => this.#filterRootMessage(msg))
      .on("collect", (msg) => processRootErr(this.#processRootMessage(msg)));
    const collection = await this.#channel.messages.fetch({
      limit: 30,
    });
    collection
      .filter((msg) => this.#filterRootMessage(msg))
      .forEach((msg) => processRootErr(this.#processRootMessage(msg)));
  };

  #filterRootMessage = (msg: Discord.Message): boolean => {
    return msg.client.user === msg.author && !isFinal(msg);
  };

  #processRootMessage = async (msg: Discord.Message): Promise<void> => {
    const title = msg.embeds[0].title;
    if (title === requested) {
      msg
        .createReactionCollector((r) => this.#filterRequested(r), {
          max: 1,
        })
        .once("collect", (r, u) =>
          processRequestedErr(this.#processRequested(r, u))
        );
      await msg.react(claimEmoji);
    } else if (title === responding) {
      msg
        .createReactionCollector((r, u) => this.#filterResponding(r, u), {
          max: 1,
        })
        .once("collect", (r) =>
          processRespondingErr(this.#processResponding(r))
        );
      await msg.react(completeEmoji);
      await msg.react(unclaimEmoji);
    }
  };

  #filterRequested = (reaction: Discord.MessageReaction): boolean => {
    return !reaction.me && reaction.emoji.name === claimEmoji;
  };

  #processRequested = async (
    reaction: Discord.MessageReaction,
    user: Discord.User | Discord.PartialUser
  ): Promise<void> => {
    const newMsg = await setTicketResponding(reaction.message, user);
    this.#notifier.invoke(newMsg);
    await this.#processRootMessage(newMsg);
  };

  #filterResponding = (
    reaction: Discord.MessageReaction,
    user: Discord.User | Discord.PartialUser
  ): boolean => {
    return (
      getMentor(reaction.message) === user.id &&
      (reaction.emoji.name === completeEmoji ||
        reaction.emoji.name === unclaimEmoji)
    );
  };

  #processResponding = async (
    reaction: Discord.MessageReaction
  ): Promise<void> => {
    if (reaction.emoji.name === completeEmoji) {
      this.#notifier.invoke(await setTicketCompleted(reaction.message));
    } else if (reaction.emoji.name === unclaimEmoji) {
      const newMsg = await setTicketRequested(reaction.message);
      this.#notifier.invoke(newMsg);
      await this.#processRootMessage(newMsg);
    }
  };
}
