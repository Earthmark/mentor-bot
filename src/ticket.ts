import Discord from "discord.js";

import { toStr } from "./req.js";

// This is the main adaptation layer between Discord and the service,
// ticket operations are routed through this file.

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

type NeosMentor = {
  name: string;
  neosId: string;
};

export type TicketCreateArgs = {
  [Field in keyof typeof createFields]: string | undefined;
};

const createFields = {
  name: "User",
  lang: "Language",
  desc: "Description",
  session: "Session",
  sessionId: "Session ID",
  sessionUrl: "Session Url",
  sessionWebUrl: "Session Web Url",
};

const fieldNames = {
  ...createFields,
  mentorName: "Mentor Name",
  mentorDiscordId: "Mentor",
  mentorNeosId: "Mentor Neos Id",
  created: "Created",
  claimed: "Claimed",
  completed: "Completed",
  canceled: "Canceled",
};

// The source that stores tickets.
export interface TicketStore {
  observeTickets: (handler: (ticket: Ticket) => void) => () => boolean;
  scanTickets: (
    limit: number,
    handler: (ticket: Ticket) => void
  ) => Promise<void>;
  createTicket: (body: TicketCreateArgs) => Promise<Ticket>;
  getTicket: (ticket: string) => Promise<Ticket>;
}

// A ticket from a mentee requesting the assistance of a mentor.
export interface Ticket {
  id: string;
  getDiscordMentor: () => string | undefined;
  isCompleted: () => boolean;
  getStatus: () => Status | "unknown";
  toMenteePayload: (accept: string | undefined) => string;
  toMentorPayload: (accept: string | undefined) => string;

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
  token: string,
  channel: string
): Promise<TicketStore> => {
  const client = new Discord.Client();

  await client.login(token);

  try {
    const chan = await client.channels.fetch(channel);
    if (!chan || !chan.isText()) {
      throw new Error("Bound to invalid channel.");
    }
    return new DiscordTicketStore(chan);
  } catch (e) {
    client.destroy();
    throw new Error(
      "BOT_CHANNEL was not provided or did not connect to an accessible channel."
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
        const ticket = this.#tryBindTicket(msg);
        if (ticket) {
          handler(ticket);
        }
      });
  };

  createTicket = async (body: TicketCreateArgs): Promise<Ticket> => {
    const embed = bodyToEmbed(body);
    const msg = await this.#channel.send(embed);
    await Promise.all([
      msg.react(claimEmoji),
      msg.react(unclaimEmoji),
      msg.react(completeEmoji),
    ]);
    const ticket = this.#tryBindTicket(msg);
    return ticket;
  };

  getTicket = async (ticket: string): Promise<Ticket> => {
    return this.#tryBindTicket(await this.#channel.messages.fetch(ticket));
  };

  #tryBindTicket = (message: Discord.Message): Ticket => {
    const embed = message.embeds[0];
    if (!embed) {
      throw new Error("Message did not have an embed, likely invalid.");
    }
    const title = embed.title;
    if (title === null || statusMap[title] === undefined) {
      throw new Error("Message embed did not have a title, likely invalid.");
    }
    return new DiscordTicket(message);
  };
}

// A ticket backed by a discord message.
class DiscordTicket {
  #ticket: Discord.Message;

  id: string;

  constructor(ticket: Discord.Message) {
    this.#ticket = ticket;
    this.id = this.#ticket.id;
  }

  getDiscordMentor = (): Discord.Snowflake | undefined =>
    this.#ticket.embeds[0].fields
      .find((f) => f.name === fieldNames.mentorDiscordId)
      ?.value.replace("<@", "")
      .replace(">", "");

  isCompleted = (): boolean => {
    const title = this.#ticket.embeds[0].title;
    return title !== requested && title !== responding;
  };

  toMenteePayload = (accept: string | undefined): string => {
    const embed = this.#ticket.embeds[0];
    const mentor = embed.fields.find(
      (f) => f.name === fieldNames.mentorName
    )?.value;
    const mentorNeosId = embed.fields.find(
      (f) => f.name === fieldNames.mentorNeosId
    )?.value;

    const toSend: {
      ticket: string;
      status: string;
      mentor?: string;
      mentorNeosId?: string;
    } = {
      ticket: this.#ticket.id,
      status: this.getStatus(),
      mentor,
      mentorNeosId,
    };

    return toStr(toSend, accept);
  };
  toMentorPayload = (accept: string | undefined): string => {
    const embed = this.#ticket.embeds[0];
    const session = embed.fields.find(
      (f) => f.name === fieldNames.session
    )?.value;
    const sessionId = embed.fields.find(
      (f) => f.name === fieldNames.sessionId
    )?.value;

    const toSend: {
      ticket: string;
      status: string;
      session?: string;
      sessionId?: string;
    } = {
      ticket: this.#ticket.id,
      status: this.getStatus(),
      session,
      sessionId,
    };

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
      await this.#editTicket((embed) =>
        embed
          .setTitle(canceled)
          .addField(fieldNames.canceled, new Date(Date.now()))
      );
    }
    return this;
  };

  setResponding = async (
    mentor: Discord.User | Discord.PartialUser | NeosMentor
  ): Promise<Ticket> => {
    if (this.getStatus() === "requested") {
      await this.#editTicket((embed) =>
        embed
          .setTitle(responding)
          .spliceFields(
            1,
            0,
            "neosId" in mentor
              ? [
                  {
                    name: fieldNames.mentorName,
                    value: mentor.name,
                    inline: true,
                  },
                  {
                    name: fieldNames.mentorNeosId,
                    value: mentor.neosId,
                    inline: true,
                  },
                ]
              : [
                  {
                    name: fieldNames.mentorDiscordId,
                    value: mentor,
                    inline: true,
                  },
                  {
                    name: fieldNames.mentorName,
                    value: mentor.username,
                    inline: true,
                  },
                ]
          )
          .addField(fieldNames.claimed, new Date(Date.now()))
      );
    }
    return this;
  };

  setRequested = async (): Promise<Ticket> => {
    if (this.getStatus() === "responding") {
      await this.#editTicket((embed) =>
        removeFields(embed, [
          fieldNames.claimed,
          fieldNames.mentorName,
          fieldNames.mentorDiscordId,
          fieldNames.mentorNeosId,
        ]).setTitle(requested)
      );
    }
    return this;
  };

  setCompleted = async (): Promise<Ticket> => {
    if (this.getStatus() === "responding") {
      await this.#editTicket((embed) =>
        embed
          .setTitle(completed)
          .addField(fieldNames.completed, new Date(Date.now()))
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
            user.id === this.getDiscordMentor()),
        { max: 1 }
      )
      .once("collect", (reaction, user) =>
        emoji[reaction.emoji.name].handler(user)
      );
  };
}

const bodyToEmbed = (body: TicketCreateArgs): Discord.MessageEmbed => {
  const result: Record<string, string> = {};
  let key: keyof typeof createFields;
  for (key in createFields) {
    const field = body[key];
    if (field) {
      result[key] = field;
    }
  }

  if (!result.name || !result.session) {
    throw new Error("A field was invalid");
  }

  const embed = Object.keys(result)
    .reduce(
      (prev, cur) =>
        prev.addField(
          fieldNames[cur as keyof typeof createFields],
          result[cur]
        ),
      new Discord.MessageEmbed().setTitle(requested)
    )
    .addField("Created", new Date(Date.now()));

  return embed;
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
