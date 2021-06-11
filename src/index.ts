import Discord from "discord.js";
import express from "express";
import dotenv from "dotenv";
import { observeChannel } from "./discord_binding";
import {requested, responding, canceled, completed} from "./ticket";

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
    .addField("Created", new Date(Date.now())) : undefined;
}

function embedTitleToStatus(embed: Discord.MessageEmbed): string {
  let status = "unknown";
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
    case completed:
      status = "completed";
      break;
  }
  return status;
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

app.use(function formatResponse(req, res, next){
  res.locals.writer = async (msg: Discord.Message): Promise<void> => {
    const embed = msg.embeds[0];
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
  
    const toSend: {
      ticket: string,
      status: string,
      mentor?: string,
    } = {
      ticket: msg.id,
      status: embedTitleToStatus(embed),
    };
  
    if (mentor) {
      toSend.mentor = mentor;
    }

    if (req.accepts("json") && req.headers.accept) {
      res.send(toSend);
    } else {
      res.send(new URLSearchParams(toSend).toString());
    }
  }
  next();
});

function getWriter(res: any): (embed: Discord.Message) => Promise<void> {
  return res.locals.writer;
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

  await getWriter(res)(message);
});

app.get("/mentee/:ticket", async function CheckTicket(req, res) {
  await getWriter(res)(getMessage(res));
});

app.post("/mentee/:ticked/cancel", async function DeleteTicket(req, res) {
  const msg = getMessage(res);
  const embed = getEmbed(res);

  if (embed.title !== requested) {
    res.sendStatus(400);
    return;
  }

  const newMsg = await msg.edit(new Discord.MessageEmbed(embed).setTitle(canceled));

  await getWriter(res)(newMsg);
});

app.listen(process.env.PORT);

getChannel().then(chan => {
  if (chan) {
    observeChannel(chan);
  }
});
