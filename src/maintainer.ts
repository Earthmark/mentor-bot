import Discord from "discord.js";
import { requested, responding, claimEmoji, unclaimEmoji, completeEmoji, getMentor, channel, isFinal, setTicketResponding, setTicketCompleted, setTicketRequested } from './ticket';
import { invokeSubscriptions } from './subs';

export async function observeChannel(chan: Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel): Promise<void> {
  chan.createMessageCollector(messageFilter).on('collect', messageProcess);
  const collection = await chan.messages.fetch({
    limit: 30,
  });
  collection.filter(messageFilter).forEach(messageProcess);
}

function messageFilter(msg: Discord.Message): boolean {
  return msg.client.user === msg.author && !isFinal(msg);
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
  const newMsg = await setTicketResponding(reaction.message, user);
  invokeSubscriptions(newMsg);
  messageProcess(newMsg);
}


function respondingFilter(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser): boolean {
  return getMentor(reaction.message) === user.id && (reaction.emoji.name === completeEmoji || reaction.emoji.name === unclaimEmoji);
}

async function respondingProcess(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser) {
  if (reaction.emoji.name === completeEmoji) {
    invokeSubscriptions(await setTicketCompleted(reaction.message));
  } else if (reaction.emoji.name === unclaimEmoji) {
    const newMsg = await setTicketRequested(reaction.message);
    invokeSubscriptions(newMsg);
    messageProcess(newMsg);
  }
}

channel.then(chan => {
  if (chan) {
    observeChannel(chan);
  }
});
