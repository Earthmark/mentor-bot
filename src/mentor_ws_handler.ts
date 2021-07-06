import { Ticket, TicketStore } from "./ticket";
import { Subscriber } from "./channel";
import { logProm } from "./prom_catch";
import { WsHandler } from "./httpServer";
import { toObj } from "./req";

// Websocket handling is defined here, but this is not the actual server.

const inboundHandler = logProm("Error while handling inbound mentor request");

export default ({
  store,
  subscriber,
}: {
  store: TicketStore;
  subscriber: Subscriber<Ticket>;
}): WsHandler => {
  // TODO: Verify these arguments more aggressively.
  return async (args, accept, outboundHandler, _close) => {
    const name = args.name;
    const neosId = args.neosId;
    var ticketId: string | undefined = args.ticket;

    if (!name || !neosId) {
      throw new Error("Fields name and neosId not provided.");
    }

    _close();

    return {
      inboundHandler: (msg) =>
        inboundHandler(async () => {
          const m = toObj(msg) as {
            type: "claim" | "complete" | "unclaim";
          };
          if (ticketId !== undefined) {
            const ticket = await store.getTicket(ticketId);
            if (m.type === "claim") {
              outboundHandler(
                (
                  await ticket.setResponding({
                    name,
                    neosId,
                  })
                ).toMenteePayload(accept)
              );
            } else if (m.type === "complete") {
              outboundHandler(
                (await ticket.setCompleted()).toMenteePayload(accept)
              );
            } else if (m.type === "unclaim") {
              outboundHandler(
                (await ticket.setRequested()).toMenteePayload(accept)
              );
            }
          }
        }),
      onClose: () => {}, //notifier.unsubscribe(sub),
    };
  };
};
