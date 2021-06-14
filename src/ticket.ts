import Discord from "discord.js";
import dotenv from "dotenv";

export const requested = 'Mentor Requested';
export const responding = 'Mentor Responding';
export const completed = 'Request Completed';
export const canceled = 'Request Canceled';

export const claimEmoji = '👌';
export const unclaimEmoji = '🚫';
export const completeEmoji = '✅';


dotenv.config();

export const client = new Discord.Client();

const clientLogin = client.login(process.env.BOT_TOKEN);

export async function getChannel() : Promise<Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel | undefined> {
  await clientLogin;
  const chan = await client.channels.fetch(process.env.BOT_CHANNEL ?? "");
  if (chan === undefined || !chan.isText()) {
    return undefined;
  }
  return chan;
}

export function getMentor(message: Discord.Message): Discord.Snowflake | undefined {
  return message.embeds[0].fields
    .find(f => f.name === "Mentor")?.value
    .replace("<@", "").replace(">", "");
}
