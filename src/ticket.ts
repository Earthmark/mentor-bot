import Discord from "discord.js";

import { toStr } from "./req";

export const requested = "Mentor Requested";
export const responding = "Mentor Responding";
export const completed = "Request Completed";
export const canceled = "Request Canceled";

type Status = "requested" | "responding" | "completed" | "canceled";

export const claimEmoji = "👌";
export const completeEmoji = "✅";
export const unclaimEmoji = "🚫";

const statusMap: {
  [key: string]: Status;
} = {
  [requested]: "requested",
  [responding]: "responding",
  [canceled]: "canceled",
  [completed]: "completed",
};

export const createStore = async (token: string): Promise<TicketStore> => {
  const client = new Discord.Client();

  await client.login(token);

  const chan = await client.channels.fetch(process.env.BOT_CHANNEL ?? "");
  if (chan === undefined || !chan.isText()) {
    throw new Error("Bound to invalid channel.");
  }
  return new TicketStore(chan);
};

export class TicketStore {
  #channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel;
  constructor(
    channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel
  ) {
    this.#channel = channel;
  }

  observeTickets = (handler: (ticket: Ticket) => void): void => {
    this.#channel
      .createMessageCollector((msg) => msg.client.user === msg.author)
      .on("collect", (msg) => {
        const ticket = this.#tryBindTicket(msg);
        if (ticket) {
          handler(ticket);
        }
      });
  };

  scanTickets = async (
    limit: number,
    handler: (ticket: Ticket) => void
  ): Promise<void> => {
    const collection = await this.#channel.messages.fetch({
      limit,
    });
    collection
      .filter((msg) => msg.client.user === msg.author)
      .forEach((msg) => {
        const ticket = this.#tryBindTicket(msg);
        if (ticket) {
          handler(ticket);
        }
      });
  };

  getOrCreateTicket = (body: {
    [key: string]: string;
  }): Promise<Ticket | undefined> => {
    const ticket = body.ticket;

    if (ticket) {
      return this.getTicket(ticket);
    }

    return this.#createTicket(body);
  };

  getTicket = (ticket: string): Promise<Ticket | undefined> => {
    return this.#channel.messages.fetch(ticket).then(this.#tryBindTicket);
  };

  #tryBindTicket = (message: Discord.Message): Ticket | undefined => {
    const embed = message.embeds[0];
    if (!embed) {
      return undefined;
    }
    const title = embed.title;
    if (title === null || statusMap[title] === undefined) {
      return undefined;
    }
    return new Ticket(message);
  };

  #createTicket = async (body: {
    [key: string]: string;
  }): Promise<Ticket | undefined> => {
    const embed = bodyToEmbed(body);
    const ticket = await this.#channel.send(embed).then(this.#tryBindTicket);
    if (ticket) {
      ticket.addReaction(claimEmoji);
      ticket.addReaction(completeEmoji);
      ticket.addReaction(unclaimEmoji);
    }
    return ticket;
  };
}

export class Ticket {
  #ticket: Discord.Message;

  constructor(ticket: Discord.Message) {
    this.#ticket = ticket;
  }

  getId = (): string => this.#ticket.id;

  getMentor = (): Discord.Snowflake | undefined =>
    this.#ticket.embeds[0].fields
      .find((f) => f.name === "Mentor")
      ?.value.replace("<@", "")
      .replace(">", "");

  isCompleted = (): boolean => {
    const title = this.#ticket.embeds[0].title;
    return title !== requested && title !== responding;
  };

  toPayload = (accept: string | undefined): string => {
    const embed = this.#ticket.embeds[0];
    const mentor = embed.fields.find((f) => f.name === "Mentor Name")?.value;

    const toSend: {
      ticket: string;
      status: string;
      mentor?: string;
    } = {
      ticket: this.#ticket.id,
      status: this.getStatus(),
    };

    if (mentor) {
      toSend.mentor = mentor;
    }

    return toStr(toSend, accept);
  };

  addReaction = async (emote: string): Promise<Ticket> => {
    this.#ticket = (await this.#ticket.react(emote)).message;
    return this;
  };

  getStatus = (): Status | "unknown" => {
    return statusMap[this.#ticket.embeds[0].title ?? ""] ?? "unknown";
  };

  #editTicket = async (
    mutator: (embed: Discord.MessageEmbed) => Discord.MessageEmbed
  ): Promise<void> => {
    this.#ticket = await this.#ticket.edit(
      mutator(new Discord.MessageEmbed(this.#ticket.embeds[0]))
    );
  };

  setCanceled = async (): Promise<Ticket> => {
    if (this.getStatus() === "requested" || this.getStatus() === "responding") {
      await this.#editTicket((embed) => embed.setTitle(canceled));
    }
    return this;
  };

  setResponding = async (
    mentor: Discord.User | Discord.PartialUser
  ): Promise<Ticket> => {
    if (this.getStatus() === "requested") {
      await this.#editTicket((embed) =>
        embed
          .setTitle(responding)
          .spliceFields(1, 0, [
            { name: "Mentor", value: mentor, inline: true },
            { name: "Mentor Name", value: mentor.username, inline: true },
          ])
          .addField("Claimed", new Date(Date.now()))
      );
    }
    return this;
  };

  setCompleted = async (): Promise<Ticket> => {
    if (this.getStatus() === "responding") {
      await this.#editTicket((embed) =>
        embed.setTitle(completed).addField("Completed", new Date(Date.now()))
      );
    }
    return this;
  };

  setRequested = async (): Promise<Ticket> => {
    if (this.getStatus() === "responding") {
      await this.#editTicket((embed) =>
        removeFields(embed, ["Mentor", "Claimed", "Mentor Name"]).setTitle(
          requested
        )
      );
    }
    return this;
  };

  observeForReaction = (
    emoji: {
      [key: string]: (user: Discord.User | Discord.PartialUser) => void;
    },
    mentorOnly: boolean
  ): void => {
    this.#ticket
      .createReactionCollector(
        (
          reaction: Discord.MessageReaction,
          user: Discord.User | Discord.PartialUser
        ): boolean =>
          !reaction.me &&
          emoji[reaction.emoji.name] !== undefined &&
          (!mentorOnly || user.id === this.getMentor()),
        { max: 1 }
      )
      .once("collect", (reaction, user) => emoji[reaction.emoji.name](user));
  };
}

const bodyToEmbed = (body: { [key: string]: string }): Discord.MessageEmbed => {
  let success = true;

  const migrateField = (name: string): string => {
    const val = body[name];
    if (val) {
      return val;
    }
    success = false;
    return "";
  };

  const user = migrateField("name");
  const lang = migrateField("lang");
  const desc = migrateField("desc");
  const session = migrateField("session");

  if (!success) {
    throw new Error("A field was invalid");
  }

  return new Discord.MessageEmbed()
    .setTitle(requested)
    .addField("User", user, true)
    .addField("Language", lang, true)
    .addField("Description", desc)
    .addField("Session", session)
    .addField("Created", new Date(Date.now()));
};

const removeFields = (
  embed: Discord.MessageEmbed,
  toRemove: string[]
): Discord.MessageEmbed => {
  embed.fields = embed.fields.filter(
    (field) => toRemove.find((rem) => rem === field.name) === undefined
  );
  return embed;
};
