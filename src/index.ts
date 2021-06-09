import Discord from "discord.js";
import express from "express";
import dotenv from "dotenv";

const requested = 'Mentor Requested';
const responding = 'Mentor Responding';
const canceled = 'Request Canceled';

function parseBodyToMsg(body: { [index: string]: any }) : Discord.MessageEmbed | undefined {
  var success = true;

  function migrateField(value: string | number | undefined) : string {
    if (typeof(value) === 'string' || typeof(value) === 'number') {
      return value + '';
    }
    success = false;
    return '';
  }

  const user = migrateField(body.name);
  const lang = migrateField(body.lang);
  const desc = migrateField(body.desc);
  const session = migrateField(body.session);

  return success ? new Discord.MessageEmbed()
    .setTitle(requested)
    .addField("User", user, true)
    .addField("Language", lang, true)
    .addField("Description", desc)
    .addField("Session", session)
    .setTimestamp() : undefined;
}

dotenv.config();

const client = new Discord.Client();

const clientLogin = client.login(process.env.BOT_TOKEN);

async function getChannel() : Promise<Discord.TextChannel | Discord.DMChannel | Discord.NewsChannel | undefined> {
  await clientLogin;
  const chan = await client.channels.fetch(process.env.BOT_CHANNEL ?? "");
  if (chan === undefined || !chan.isText()) {
    return undefined;
  }
  return chan;
}

const app = express();
app.use(express.urlencoded({ extended: true }));
app.use(express.json());

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

app.post("/mentee", async function CreateTicket(req, res) {
  const embed = parseBodyToMsg(req.body);

  if (!embed) {
    return res.sendStatus(400);
  }

  const chan = getChan(res);
  const message = await chan.send(embed);
  await message.react('✅');

  res.send({
    ticket: message.id,
  });
});

app.get("/mentee/:ticket", async function CheckTicket(req, res) {
  const msg = getMessage(res);
  const embed = getEmbed(res);

  let mentor = embed.fields.find(f => f.name === "Mentor")?.value;

  if (mentor) {
    mentor = mentor.replace("<@", "").replace(">", "");
    try{
      if (mentor) {
        const user = await client.users.fetch(mentor);
        mentor = user.username;
      }
    }
    catch {
      // This means user not found.
    }
  }

  let status;
  switch(embed.title) {
    case requested:
      status = "requested";
      break;
    case responding:
      status = "responding";
      break;
    case canceled:
      status = "canceled";
      break;
  }

  res.send({
    ticket: msg.id,
    status,
    mentor,
  });
});

app.post("/mentee/:ticked/cancel", async function DeleteTicket(req, res) {
  const msg = getMessage(res);
  const embed = getEmbed(res);

  if (embed.title !== requested) {
    res.sendStatus(400);
    return;
  }

  await msg.edit(new Discord.MessageEmbed(embed).setTitle(canceled));

  res.sendStatus(200);
});

app.listen(process.env.PORT);

function processReact(reaction: Discord.MessageReaction, user: Discord.User | Discord.PartialUser) {
  const embed = new Discord.MessageEmbed(reaction.message.embeds[0]);
  if (embed.title === requested) {
    reaction.message.edit(embed.setTitle(responding).spliceFields(1, 0, { name: "Mentor", value: user, inline: true }));
  }
}

getChannel().then(chan => {
  chan?.createMessageCollector((msg: Discord.Message) =>
    client.user === msg.author && msg.embeds[0]?.title === requested
    ).on('collect', (msg: Discord.Message) => {
      msg.createReactionCollector((reaction: Discord.MessageReaction) =>
        !reaction.me && reaction.emoji.name === '✅'
      ).once('collect', processReact);
  });
});
