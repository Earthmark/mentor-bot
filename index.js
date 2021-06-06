import http from "http";
import { Client } from "discord.js";
import Websocket from "ws";

const server = http.createServer();
const mentorWss = new Websocket.Server({ noServer: true });
const menteeWss = new Websocket.Server({ noServer: true });

mentorWss.on('connection', function(ws) {
  ws.on('message', function msg(msg) {
    console.log(msg);
  })
  console.log("Got Mentor");
});

menteeWss.on('connection', function(ws) {
  ws.on('message', function msg(msg) {
    console.log(msg);
  })
  console.log("Got Mentee");
});

server.on('upgrade', function upgrade(request, socket, head) {
  if (request.url == '/mentor') {
    mentorWss.handleUpgrade(request, socket, head, function done(ws) {
      mentorWss.emit('connection', ws, request);
    })
  } else if (request.url == '/mentee') {
    menteeWss.handleUpgrade(request, socket, head, function done(ws) {
      menteeWss.emit('connection', ws, request);
    })
  } else {
    socket.destroy();
  }
});

server.listen(8080);

const client = new Client();

client.on('ready', () => {
  console.log(`Ready! ${client.user?.tag}`);
});

client.on('message', msg => {
  if (msg.content === 'ping') {
    msg.reply('Pong!');
  }
});