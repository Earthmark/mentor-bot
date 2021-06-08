import http from "http";
import Discord from "discord.js";
import Websocket from "ws";
import dotenv from "dotenv";
import qs from "querystring";

async function mainMethod() {
  dotenv.config();

  const client = new Discord.Client();
  
  await client.login(process.env.BOT_TOKEN);
  
  const broadcastChannel = await client.channels.fetch(process.env.BOT_CHANNEL);
  if (!broadcastChannel.isText()) {
    throw "Bound channel was not a text channel.";
  }
  
  const menteeWss = new Websocket.Server({ noServer: true });
  
  menteeWss.on('connection', async function newConnection(ws, req) {
    const url = new URL(req.url, 'http://example.com');
    const decoded_user = qs.decode(url.search.substr(1));
    const user = {
      name: decoded_user.name,
      session: decoded_user.session,
      language: decoded_user.language,
      description: decoded_user.description,
    }
  
    if (!user.name || !user.session || !user.language || !user.description) {
      ws.close();
      return;
    }
  
    ws.on('message', async function GotMessage(msg) {
      const request = qs.decode(msg.toString());
    });
  
    const embed = new Discord.MessageEmbed()
    .setTitle(`Assistance requested`)
    .addFields(
      { name: "User", value: user.name, inline: true },
      { name: "Language", value: user.language, inline: true },
      { name: "Description", value: user.description },
      { name: "Session", value: user.session },
    )
    .setTimestamp();
  
    const sent = await broadcastChannel.send(embed);
    await sent.react('✅');
    const reactions = await sent.awaitReactions((reaction, user) => {
      return reaction.emoji.name === '✅' && user.id !== sent.author.id;
    }, { max: 1, time: 60000, errors: ["time"] });
    const reacterId = reactions.first().users.cache.find(user => !user.bot);
  
    const requestClaimed = new Discord.MessageEmbed(embed)
      .setTitle(`Mentor On Site`)
      .addField('Responding Mentor', `${reacterId}`);
    await sent.edit(requestClaimed);
  });
  
  const server = http.createServer();
  server.on('upgrade', function upgrade(request, socket, head) {
    const url = new URL(request.url, 'http://example.com');
    if (url.pathname == '/mentee') {
      menteeWss.handleUpgrade(request, socket, head, function done(ws) {
        menteeWss.emit('connection', ws, request);
      })
    } else {
      socket.destroy();
    }
  });
  
  server.listen(process.env.PORT);
}

mainMethod();