import Discord, { Snowflake } from "discord.js";
import dotenv from "dotenv";
import {toStr} from './req';

export const requested = 'Mentor Requested';
export const responding = 'Mentor Responding';
export const completed = 'Request Completed';
export const canceled = 'Request Canceled';

export const claimEmoji = '👌';
export const unclaimEmoji = '🚫';
export const completeEmoji = '✅';

dotenv.config();

const client = new Discord.Client();

const clientLogin = client.login(process.env.BOT_TOKEN);

export const channel = clientLogin.then(async () => {
  const chan = await client.channels.fetch(process.env.BOT_CHANNEL ?? "");
  if (chan === undefined || !chan.isText()) {
    throw new Error("Bound to invalid channel.");
  }
  return chan;
});

export function getMentor(message: Discord.Message): Discord.Snowflake | undefined {
  return message.embeds[0].fields
    .find(f => f.name === "Mentor")?.value
    .replace("<@", "").replace(">", "");
}

export function isFinal(message: Discord.Message): boolean {
  if (!message.embeds[0]) {
    throw new Error("Ticket did not have en embed, likely a bad id.");
  }
  const embed = message.embeds[0];
  const title = embed.title;
  return (title !== requested && title !== responding);
}

export function getOrCreateTicket(body: { [key: string]: string }): Promise<Discord.Message> {
  const ticket = body.ticket;

  if (ticket) {
    return getTicket(ticket);
  }

  return createTicket(body);
}

function getTicket(ticket: Snowflake): Promise<Discord.Message> {
  return channel.then(cha => cha.messages.fetch(ticket));
}

function createTicket(body: { [key: string]: string }): Promise<Discord.Message> {
  const ticket = parseNewTicket(body);
  return channel.then(cha => cha.send(ticket));
}

function parseNewTicket(body:  { [key: string]: string }) : Discord.MessageEmbed {
  let success = true;

  function migrateField(name: string) : string {
    const val = body[name];
    if (val) {
      return val;
    }
    success = false;
    return '';
  }

  const user = migrateField('name');
  const lang = migrateField('lang');
  const desc = migrateField('desc');
  const session = migrateField('session');

  if (!success){
    throw new Error("A field was invalid");
  }

  return new Discord.MessageEmbed()
    .setTitle(requested)
    .addField("User", user, true)
    .addField("Language", lang, true)
    .addField("Description", desc)
    .addField("Session", session)
    .addField("Created", new Date(Date.now()));
}

const statusMap: {
  [key: string]: string
} = {
  [requested]: "requested",
  [responding]: "responding",
  [canceled]: "canceled",
  [completed]: "completed",
}

export function messageToString(msg: Discord.Message, accept?: string): string {
  const embed = msg.embeds[0];
  const mentor = embed.fields.find(f => f.name === "Mentor Name")?.value;

  const toSend: {
    ticket: string,
    status: string,
    mentor?: string,
  } = {
    ticket: msg.id,
    status: statusMap[embed.title ?? ''] ?? "unknown",
  };

  if (mentor) {
    toSend.mentor = mentor;
  }

  return toStr(toSend, accept);
}

export async function setTicketCanceled(ticket: Snowflake): Promise<Discord.Message> {
  const message = await getTicket(ticket);
  if (isFinal(message)) {
    throw new Error("ticket is in a final state.");
  }
  return await message.edit(new Discord.MessageEmbed(message.embeds[0]).setTitle(canceled));
}

export function setTicketResponding(message: Discord.Message, mentor: Discord.User | Discord.PartialUser): Promise<Discord.Message> {
  const embed = new Discord.MessageEmbed(message.embeds[0]);
  if (embed.title !== requested) {
    throw new Error("Ticket was not in requested state");
  }
  return message.edit(embed
    .setTitle(responding)
    .spliceFields(1, 0, [{ name: "Mentor", value: mentor, inline: true }, { name: "Mentor Name", value: mentor.username, inline: true }])
    .addField("Claimed", new Date(Date.now()))
  );
}

export function setTicketCompleted(message: Discord.Message): Promise<Discord.Message> {
  const embed = new Discord.MessageEmbed(message.embeds[0]);
  if (embed.title !== responding) {
    throw new Error("Ticket was not in a responding state");
  }
  return message.edit(embed
    .setTitle(completed)
    .addField("Completed",new Date(Date.now()))
  )
}

function removeFields(embed: Discord.MessageEmbed, toRemove: string[]): Discord.MessageEmbed {
  embed.fields = embed.fields.filter(
    field => toRemove.find(rem => rem === field.name) === undefined)
  return embed;
}

export function setTicketRequested(message: Discord.Message): Promise<Discord.Message> {
  const embed = new Discord.MessageEmbed(message.embeds[0]);
  if (embed.title !== responding) {
    throw new Error("Ticket was not in a responding state");
  }
  return message.edit(removeFields(embed, ["Mentor", "Claimed", "Mentor Name"])
        .setTitle(requested)
      );
}
