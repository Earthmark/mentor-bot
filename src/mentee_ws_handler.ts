import { Ticket, TicketStore, TicketGetOrCreateArgs } from "./ticket.js";
import { SubscriptionNotifier } from "./subs.js";
import { logProm } from "./prom_catch.js";

// Websocket handling is defined here, but this is not the actual server.

type MenteeRequest = Ticket;

export type MenteeResponse = {
  type: "cancel";
};

export type MenteeHandler = (
  getOrCreate: TicketGetOrCreateArgs,
  outboundHandler: (req: MenteeRequest) => void,
  close: () => void
) => Promise<{
  inboundHandler: (arg: MenteeResponse) => void;
  onClose: () => void;
}>;

const inboundHandler = logProm("Error while handling inbound mentee request");

// This represents a websocket server clients can create tickets through.
export const createWsServer = (data: {
  store: TicketStore;
  notifier: SubscriptionNotifier<Ticket>;
  stopDelay: number;
}): MenteeHandler => {
  const store = data.store;
  const notifier = data.notifier;
  const stopDelay = data.stopDelay;

  return async (getOrCreate, outboundHandler, close) => {
    const ticket = await store.getOrCreateTicket(getOrCreate);

    const id = ticket.id;

    const checkCanceled = (ticket: Ticket): void => {
      if (ticket.isCompleted()) {
        // Due to a bug in the neos client, send the message first, then cancel the websocket a bit later.
        // In this case 10 seconds.
        setTimeout(() => close(), stopDelay);
      }
    };

    const broadcastTicket = (t: Ticket): void => {
      outboundHandler(t);
      checkCanceled(t);
    };

    broadcastTicket(ticket);

    const sub = notifier.subscribe(id, broadcastTicket);

    return {
      inboundHandler: (msg) =>
        inboundHandler(async () => {
          if (msg.type === "cancel") {
            const ticket = await store.getTicket(id);
            const canceledTicket = await ticket.setCanceled();
            notifier.invoke(canceledTicket);
          } else {
            throw new Error("Unsupported message type.");
          }
        }),
      onClose: () => notifier.unsubscribe(sub),
    };
  };
};
