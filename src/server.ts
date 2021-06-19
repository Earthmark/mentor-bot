import Discord from 'discord.js';
import Websocket from 'ws';

import { getOrCreateTicket, messageToString, setTicketCanceled, isFinal } from './ticket';
import { subscribeUpdates, unsubscribeUpdates, invokeSubscriptions } from './subs';
import { toObj } from './req';

const ws = new Websocket.Server({
  port: process.env.PORT as any
});

ws.on('connection', async function open(s, msg) {
  if (!msg.url?.startsWith("/mentee?")) {
    s.close();
    return;
  }

  const ticket = await getOrCreateTicket(Object.fromEntries(
    new URL("ws://localhost" + msg.url).searchParams.entries()));
  if (!ticket) {
    s.close();
    return;
  }

  const accept = msg.headers.accept;

  s.on('message', async (data) => {
    if (typeof data === 'string') {
      const val = toObj(data, accept);
      if(val.type === "cancel") {
        invokeSubscriptions(await setTicketCanceled(ticket.id));
      }
    }
  });

  s.send(messageToString(ticket, accept));

  const checkIfCanceled = (message: Discord.Message) => {
    if (isFinal(message)) {
      s.close();
    }
  }

  checkIfCanceled(ticket);

  const sub = subscribeUpdates(ticket.id, (subMsg) => {
    s.send(messageToString(subMsg, accept));
    checkIfCanceled(ticket);
  });

  s.on('close', () => {
    unsubscribeUpdates(sub);
  });
});
