import Discord from "discord.js";

export const requested = 'Mentor Requested';
export const responding = 'Mentor Responding';
export const completed = 'Request Completed';
export const canceled = 'Request Canceled';

export const claimEmoji = '👌';
export const unclaimEmoji = '🚫';
export const completeEmoji = '✅';

export function getMentor(message: Discord.Message): Discord.Snowflake | undefined {
  return message.embeds[0].fields
    .find(f => f.name === "Mentor")?.value
    .replace("<@", "").replace(">", "");
}
