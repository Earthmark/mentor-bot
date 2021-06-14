import Discord from "discord.js";
import { requested, responding, claimEmoji, unclaimEmoji, completeEmoji, getMentor, completed, getChannel } from './ticket';

export async function observeChannel(channel: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel): Promise<void> {
  channel.createMessageCollector(messageFilter).on('collect', messageProcess);
  const collection = await channel.messages.fetch({
    limit: 30,
  });
  collection.filter(messageFilter).forEach(messageProcess);
}

function messageFilter(msg: Discord.Message): boolean {
  const title = msg.embeds[0]?.title;
  return msg.client.user === msg.author && (title === requested || title === responding);
}

async function messageProcess(msg: Discord.Message) {
  const title = msg.embeds[0].title;
  if (title === requested) {
    msg.createReactionCollector(requestedFilter, {
      max: 1,
    }).once('collect', requestedProcess);
    await msg.react(claimEmoji);
  } else if (title === responding) {
    msg.createReactionCollector(respondingFilter, {
      max: 1,
    }).once('collect', respondingProcess);
    await msg.react(completeEmoji);
    await msg.react(unclaimEmoji);
  }
}

function requestedFilter(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser): boolean {
  return !reaction.me && reaction.emoji.name === claimEmoji;
}

async function requestedProcess(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser) {
  const embed = new Discord.MessageEmbed(reaction.message.embeds[0]);
  if (embed.title === requested) {
    const newMsg = await reaction.message.edit(embed
      .setTitle(responding)
      .spliceFields(1, 0, [{ name: "Mentor", value: user, inline: true }, { name: "Mentor Name", value: user.username, inline: true }])
      .addField("Claimed", new Date(Date.now()))
    );
    await messageProcess(newMsg);
  }
}

function respondingFilter(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser): boolean {
  return getMentor(reaction.message) === user.id && (reaction.emoji.name === completeEmoji || reaction.emoji.name === unclaimEmoji);
}

function removeFields(embed: Discord.MessageEmbed, toRemove: string[]): Discord.MessageEmbed {
  embed.fields = embed.fields.filter(
    field => toRemove.find(rem => rem === field.name) === undefined)
  return embed;
}

async function respondingProcess(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser) {
  const embed = new Discord.MessageEmbed(reaction.message.embeds[0]);
  if (embed.title === responding) {
    if (reaction.emoji.name === completeEmoji) {
      await reaction.message.edit(embed
        .setTitle(completed)
        .addField("Completed",new Date(Date.now()))
      );
    } else if (reaction.emoji.name === unclaimEmoji) {
      const newMsg = await reaction.message.edit(removeFields(embed, ["Mentor", "Claimed", "Mentor Name"])
        .setTitle(requested)
      );
     await  messageProcess(newMsg);
    }
  }
}

getChannel().then(chan => {
  if (chan) {
    observeChannel(chan);
  }
});
