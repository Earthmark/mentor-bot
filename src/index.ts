import Discord from "discord.js";
import express from "express";
import dotenv from "dotenv";

interface RequestTicket  {
  status: 'requested',
  name: string,
  lang: string,
  desc: string,
  session: string,
  requestTime: Date,
}

interface RespondedTicket  {
  status: 'responding',
  name: string,
  lang: string,
  desc: string,
  session: string,
  mentor: string;
  requestTime: Date,
  responseTime: Date,
}

interface CompletedTicket  {
  status: 'completed',
  name: string,
  lang: string,
  desc: string,
  mentor: string;
  requestTime: Date,
  responseTime: Date,
  completedTime: Date,
}

interface CaneledTicket {
  status: 'canceled' | 'timedOut',
  name: string,
  lang: string,
  desc: string,
  mentor?: string;
  requestTime: Date,
  responseTime?: Date,
  closedTime: Date,
}

type Ticket = RequestTicket | RespondedTicket | CompletedTicket | CaneledTicket;

type AllKeys<T> = T extends any ? keyof T : never;

type TicketStatus = Ticket['status'];

function invert<K extends number | string, V extends number | string>(objs: {[k in K]: V}) : {[k in V]: K} {
  return Object.keys(objs).reduce((obj, key) => {
    (obj as any)[(objs as any)[key]] = key;
    return obj;
  }, {}) as any;
}

const statusToTitle: {
  [Property in TicketStatus]: string
} = { 
  requested: 'Mentor Requested',
  responding: 'Mentor Responding',
  completed: 'Request Completed',
  canceled: 'Request Canceled',
  timedOut: 'Request Timed Out',
};

const titleToStatus = invert(statusToTitle);

type FieldNames = Exclude<AllKeys<Ticket>, 'status'>;

const fieldToName :{ 
  [Property in FieldNames]: string
}= {
  name: "User",
  mentor: "Mentor",
  lang: "Language",
  desc: "Description",
  session: "Session",
  requestTime: "Requested",
  responseTime: "Responded",
  completedTime: "Completed",
  closedTime: "Closed",
}

const nameToField = invert(fieldToName);

const fieldTypeMapping :Partial<{ 
  [Property in FieldNames]: boolean
}> = {
  name: true,
  mentor: true,
  lang: true,
}

function getTicketKeys(): [FieldNames] {
  return Object.keys(fieldToName) as any;
}

function embedToTicket(embed: Discord.MessageEmbed) : Ticket | undefined {
  const status: TicketStatus | undefined = titleToStatus[embed.title ?? ""];

  if (!status) {
    return undefined;
  }

  const obj = embed.fields.reduce<{[key: string]: string}>((obj, field) => {
    obj[nameToField[field.name]] = field.value;
    return obj;
  },{});

  if (status === 'requested') {
    return {
      status: 'requested',
      name: obj.name,
      lang: obj.lang,
      desc: obj.desc,
      session: obj.session,
      requestTime: new Date(Date.parse(obj.requestTime)),
    };
  } else if (status === 'responding') {
    return {
      status: 'responding',
      name: obj.name,
      lang: obj.lang,
      desc: obj.desc,
      session: obj.session,
      mentor: obj.mentor,
      requestTime: new Date(Date.parse(obj.requestTime)),
      responseTime: new Date(Date.parse(obj.responseTime)),
    };
  } else if (status === 'completed') {
    return {
      status: 'completed',
      name: obj.name,
      lang: obj.lang,
      desc: obj.desc,
      mentor: obj.mentor,
      requestTime: new Date(Date.parse(obj.requestTime)),
      responseTime: new Date(Date.parse(obj.responseTime)),
      completedTime: new Date(Date.parse(obj.completedTime)),
    };
  } else if (status === 'canceled' || status === 'timedOut') {
    return {
      status,
      name: obj.name,
      lang: obj.lang,
      desc: obj.desc,
      mentor: obj.mentor,
      requestTime: new Date(Date.parse(obj.requestTime)),
      responseTime: new Date(Date.parse(obj.responseTime)),
      closedTime: new Date(Date.parse(obj.closedTime)),
    };
  }

  return undefined;
}

function ticketToEmbed(ticket: Ticket) : Discord.MessageEmbed {
  return getTicketKeys().reduce(
    (item, cur) =>
      cur in ticket ?
        item.addField(fieldToName[cur], (ticket as any)[cur], fieldTypeMapping[cur]) : item,
    new Discord.MessageEmbed()
      .setTitle(statusToTitle[ticket.status])
      .setTimestamp());
}

function objectToRequestTicket(body: { [index: string]: any }) : RequestTicket | undefined {
  var success = true;

  function migrateField(value: string | number | undefined) : string {
    if (typeof(value) === 'string' || typeof(value) === 'number') {
      return value + '';
    }
    success = false;
    return '';
  }

  const result: RequestTicket = {
    status: 'requested',
    name: migrateField(body['name']),
    lang: migrateField(body['lang']),
    desc: migrateField(body['desc']),
    session: migrateField(body['session']),
    requestTime: new Date(Date.now()),
  };

  return success ? result : undefined;
}

function requestTicketToRespondedTicket(ticket: RequestTicket, mentor: string) : RespondedTicket {
  return {
    status: 'responding',
    name: ticket.name,
    lang: ticket.lang,
    desc: ticket.desc,
    session: ticket.session,
    mentor: mentor,
    requestTime: ticket.requestTime,
    responseTime: new Date(Date.now()),
  }
}

function respondedTicketToRequestTicket(ticket: RespondedTicket) : RequestTicket {
  return {
    status: 'requested',
    name: ticket.name,
    lang: ticket.lang,
    desc: ticket.desc,
    session: ticket.session,
    requestTime: ticket.requestTime,
  }
}

function respondedTicketToCompletedTicket(ticket: RespondedTicket) : CompletedTicket {
  return {
    status: 'completed',
    name: ticket.name,
    lang: ticket.lang,
    desc: ticket.desc,
    mentor: ticket.mentor,
    requestTime: ticket.requestTime,
    responseTime: ticket.responseTime,
    completedTime: new Date(Date.now()),
  }
}

function ticketToCanceledTicket(ticket: RequestTicket | RespondedTicket) : CaneledTicket {
  return {
    status: 'canceled',
    name: ticket.name,
    lang: ticket.lang,
    desc: ticket.desc,
    mentor: ticket.status === 'responding' ? ticket.mentor : undefined,
    requestTime: ticket.requestTime,
    responseTime: ticket.status === 'responding' ? ticket.responseTime : undefined,
    closedTime: new Date(Date.now()),
  }
}

function ticketToTimedOutTicket(ticket: RequestTicket | RespondedTicket) : CaneledTicket {
  return {
    status: 'timedOut',
    name: ticket.name,
    lang: ticket.lang,
    desc: ticket.desc,
    mentor: ticket.status === 'responding' ? ticket.mentor : undefined,
    requestTime: ticket.requestTime,
    responseTime: ticket.status === 'responding' ? ticket.responseTime : undefined,
    closedTime: new Date(Date.now()),
  }
}

dotenv.config();

const client = new Discord.Client({
  messageCacheLifetime: 5,
  messageSweepInterval: 5,
});

const clientLogin = client.login(process.env.BOT_TOKEN);

async function getChannel() : Promise<Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel | undefined> {
  await clientLogin;
  const chan = await client.channels.fetch(process.env.BOT_CHANNEL ?? "");
  if (chan === undefined || !chan.isText()) {
    return undefined;
  }
  return chan;
}

async function startWebserver() {
  await clientLogin;

  const app = express();
  app.use(express.json());
  app.use(express.urlencoded({ extended: true }));

  app.use(async function PopulateChannel(_req, res, next) {
    const chan = await getChannel();
    if (!chan) {
      return res.sendStatus(500);
    }
    res.locals.channel = chan;
    next();
  });

  function getChan(res: any) : Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel {
    return res.locals.channel;
  }

  app.use("/mentee/:ticket", async function PopulateMessageAndEmbed(req, res, next) {
    const chan = getChan(res);
    try {
      const message = await chan.messages.fetch(req.params.ticket);
      const embed = message?.embeds[0];
      if (!embed) {
        return res.sendStatus(404);
      }
      res.locals.message = message;
      res.locals.embed = embed;
      res.locals.ticket = embedToTicket(embed);
      next();
    }
    catch {
      res.sendStatus(404);
    }
  });

  function getMessage(res: any) : Discord.Message {
    return res.locals.message;
  }

  function getEmbed(res: any) : Discord.MessageEmbed {
    return res.locals.embed;
  }

  function getTicket(res: any) : Ticket {
    return res.locals.embed;
  }

  app.post("/mentee", async function CreateTicket(req, res) {
    const ticket = objectToRequestTicket(req.body);

    if (!ticket) {
      return res.sendStatus(400);
    }

    let embed = ticketToEmbed(ticket);

    const chan = getChan(res);
    const message = await chan.send(embed);
    await resetReactions(message);

    res.send({
      ticket: message.id,
    });
  });

  app.get("/mentee/:ticket", function CheckTicket(req, res) {
    const ticket = getTicket(res);

    res.send(ticket);
  });

  app.post("/mentee/:ticked/delete", async function DeleteTicket(req, res) {
    const ticket = getTicket(res);

    if(ticket.status === 'requested' || ticket.status === 'responding') {
      const canceled = ticketToCanceledTicket(ticket);
      
      const msg = getMessage(res);

      await msg.edit(msg);

      res.send(canceled);
      return;
    }

    res.sendStatus(400);
  });

  app.listen(process.env.PORT);
}

function ticketFromMessage(msg: Discord.Message): Ticket | undefined {
  const embed = msg.embeds[0];
  if (embed) {
    const ticket = embedToTicket(embed);
    return ticket;
  }
  return undefined;
}

function messageFilter(msg: Discord.Message): boolean {
  if (msg.author.bot) {
    const ticket = ticketFromMessage(msg);
    console.log(ticket);
    if (ticket){
      return ticket.status === 'responding' || ticket.status === 'requested'
    }
  }
  return false;
}

function claimFilter(reaction: Discord.MessageReaction): boolean {
  return !reaction.me && reaction.emoji.name === '👌';
}

async function requestedClaimedReactions(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser) {
  let ticket = ticketFromMessage(reaction.message);
  console.log("Ticket:", ticket);
  if (ticket?.status === 'requested') {
    if (reaction.emoji.name === '👌') {
      const responded = requestTicketToRespondedTicket(ticket, user.id);
      await reaction.message.edit(ticketToEmbed(responded));
    }
    await resetReactions(reaction.message);
    observeReactions(reaction.message);
  }
}

function claimedFilter(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser, mentor: string): boolean {
  return !reaction.me && user.id === mentor && (reaction.emoji.name === '✅' || reaction.emoji.name === '🚫');
}

async function resetReactions(message: Discord.Message) {
  await message.reactions.removeAll();
  switch(ticketFromMessage(message)?.status) {
    case 'requested':
      await message.react('👌');
      break;
    case 'responding':
      await message.react('✅');
      await message.react('🚫');
      break;
  }
}

async function observedClaimedReactions(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser) {
  let ticket = ticketFromMessage(reaction.message);
  if (ticket?.status === 'responding') {
    if (reaction.emoji.name === '✅') {
      const completed = respondedTicketToCompletedTicket(ticket);
      await reaction.message.edit(ticketToEmbed(completed));
    } else if (reaction.emoji.name === '🚫') {
      const requested = respondedTicketToRequestTicket(ticket);
      await reaction.message.edit(ticketToEmbed(requested));
    }
    await resetReactions(reaction.message);
    observeReactions(reaction.message);
  }
}

function observeReactions(msg: Discord.Message) {
  const ticket = ticketFromMessage(msg);
  if (ticket) {
    if (ticket.status === 'requested') {
      console.log("Requested Ticket:", ticket);
      const coll = msg.createReactionCollector(claimFilter, {
        max: 1
      });
      coll.on('collect', requestedClaimedReactions);
    } else if (ticket.status === 'responding') {
      console.log("Responding Ticket:", ticket);
      const coll = msg.createReactionCollector((react, user) => claimedFilter(react, user, ticket.mentor), {
        max: 1
      });
      coll.on('collect', observedClaimedReactions);
    }
  }
}

async function startMaintainer(){
  const channel = await getChannel();

  if (channel) {
    const collector = channel.createMessageCollector(messageFilter);
    collector.on('collect', observeReactions);
  } else {
    console.log("No channel.");
  }
}

startWebserver();
startMaintainer();
