import Discord from "discord.js";

import { toStr } from "./req";

export const requested = "Mentor Requested";
export const responding = "Mentor Responding";
export const completed = "Request Completed";
export const canceled = "Request Canceled";

type Status = "requested" | "responding" | "completed" | "canceled";

const statusMap: {
  [key: string]: Status;
} = {
  [requested]: "requested",
  [responding]: "responding",
  [canceled]: "canceled",
  [completed]: "completed",
};

export const claimEmoji = "👌";
export const completeEmoji = "✅";
export const unclaimEmoji = "🚫";

// The source that stores tickets.
export interface TicketStore {
  observeTickets: (handler: (ticket: Ticket) => void) => () => boolean;
  scanTickets: (
    limit: number,
    handler: (ticket: Ticket) => void
  ) => Promise<void>;
  getOrCreateTicket: (
    body: {
      ticket: string;
    } & TicketCreateArgs
  ) => Promise<Ticket | undefined>;
  getTicket: (ticket: string) => Promise<Ticket | undefined>;
}

export interface TicketCreateArgs {
  name: string;
  lang: string;
  desc: string;
  session: string;
}

// A ticket from a mentee requesting the assistance of a mentor.
export interface Ticket {
  getId: () => string;
  getMentor: () => string | undefined;
  isCompleted: () => boolean;
  getStatus: () => Status | "unknown";
  toPayload: (accept: string | undefined) => string;

  observeForReaction: (emoji: {
    [key: string]: {
      mentorOnly: boolean;
      handler: (user: Discord.User | Discord.PartialUser) => void;
    };
  }) => void;

  setCanceled: () => Promise<Ticket>;
  setResponding: (
    mentor: Discord.User | Discord.PartialUser
  ) => Promise<Ticket>;
  setCompleted: () => Promise<Ticket>;
  setRequested: () => Promise<Ticket>;
}

export const createDiscordStore = async (
  token: string
): Promise<TicketStore> => {
  const client = new Discord.Client();

  await client.login(token);

  try {
    const chan = await client.channels.fetch(process.env.BOT_CHANNEL ?? "");
    if (!chan || !chan.isText()) {
      throw new Error("Bound to invalid channel.");
    }
    return new DiscordTicketStore(chan);
  } catch (e) {
    client.destroy();
    throw new Error(
      "BOT_CHANNEL was not provided or did not connect to an accessable channel."
    );
  }
};

// The interface between discord and the ticket service.
class DiscordTicketStore {
  #channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel;
  constructor(
    channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel
  ) {
    this.#channel = channel;
  }

  observeTickets = (handler: (ticket: Ticket) => void): (() => boolean) => {
    const collection = this.#channel
      .createMessageCollector((msg) => msg.client.user === msg.author)
      .on("collect", (msg) => {
        console.log("Observed");
        const ticket = this.#tryBindTicket(msg);
        if (ticket) {
          handler(ticket);
        }
      });
    return () => !collection.ended;
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
        console.log("Scanned");
        const ticket = this.#tryBindTicket(msg);
        if (ticket) {
          handler(ticket);
        }
      });
  };

  getOrCreateTicket = (
    body: {
      ticket: string;
    } & TicketCreateArgs
  ): Promise<Ticket | undefined> => {
    if ("ticket" in body && body.ticket) {
      return this.getTicket(body.ticket);
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
    return new DiscordTicket(message);
  };

  #createTicket = async (body: {
    name: string;
    lang: string;
    desc: string;
    session: string;
  }): Promise<Ticket | undefined> => {
    const embed = bodyToEmbed(body);
    const msg = await this.#channel.send(embed);
    await Promise.all([
      msg.react(claimEmoji),
      msg.react(completeEmoji),
      msg.react(unclaimEmoji),
    ]);
    const ticket = this.#tryBindTicket(msg);
    return ticket;
  };
}

// A ticket backed by a discord message.
class DiscordTicket {
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

  observeForReaction = (emoji: {
    [key: string]: {
      mentorOnly: boolean;
      handler: (user: Discord.User | Discord.PartialUser) => void;
    };
  }): void => {
    this.#ticket
      .createReactionCollector(
        (
          reaction: Discord.MessageReaction,
          user: Discord.User | Discord.PartialUser
        ): boolean =>
          !reaction.me &&
          emoji[reaction.emoji.name] !== undefined &&
          (!emoji[reaction.emoji.name].mentorOnly ||
            user.id === this.getMentor()),
        { max: 1 }
      )
      .once("collect", (reaction, user) =>
        emoji[reaction.emoji.name].handler(user)
      );
  };
}

const bodyToEmbed = (body: TicketCreateArgs): Discord.MessageEmbed => {
  let success = true;

  const migrateField = (name: keyof TicketCreateArgs): string => {
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
