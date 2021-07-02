import { Ticket, TicketStore, TicketCreateArgs } from "./ticket";
import { Subscriber } from "./channel";
import { logProm } from "./prom_catch";
import { WsHandler } from "./httpServer";
import { toObj } from "./req";

const inboundHandler = logProm("Error while handling inbound mentee request");

// This represents a websocket server clients can create tickets through.
export default ({
  store,
  subscriber,
  stopDelay,
}: {
  store: TicketStore;
  subscriber: Subscriber<Ticket>;
  stopDelay: number;
}): WsHandler => {
  // TODO: Verify these arguments more aggressively.
  return async (args, accept, outboundHandler, close) => {
    const ticket = await ("ticket" in args && args.ticket
      ? store.getTicket(args.ticket)
      : store.createTicket(args as TicketCreateArgs));

    const id = ticket.id;

    const checkCanceled = (ticket: Ticket): void => {
      if (ticket.isCompleted()) {
        // Due to a bug in the neos client, send the message first, then cancel the websocket a bit later.
        // In this case 10 seconds.
        setTimeout(() => close(), stopDelay);
      }
    };

    const broadcastTicket = (t: Ticket): void => {
      outboundHandler(t.toMenteePayload(accept));
      checkCanceled(t);
    };

    broadcastTicket(ticket);

    const sub = subscriber.subscribe(id, broadcastTicket);

    return {
      inboundHandler: (msg) =>
        inboundHandler(async () => {
          const m = toObj(msg) as {
            type: "cancel";
          };
          if (m.type === "cancel") {
            const ticket = await store.getTicket(id);
            await ticket.setCanceled();
          } else {
            throw new Error("Unsupported message type.");
          }
        }),
      onClose: () => subscriber.unsubscribe(sub),
    };
  };
};
