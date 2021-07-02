import { Ticket, TicketStore } from "./ticket";
import { Notifier, Subscriber } from "./channel";
import { logProm } from "./prom_catch";
import { WsHandler } from "./httpServer";
import { toObj } from "./req";

// Websocket handling is defined here, but this is not the actual server.

type MentorInfo = {
  name: string;
  neosId: string;
};

const inboundHandler = logProm("Error while handling inbound mentee request");

// This represents a websocket server clients can create tickets through.
export const createWsServer = ({
  store,
  notifier,
}: {
  store: TicketStore;
  notifier: Notifier<Ticket> & Subscriber<Ticket>;
  stopDelay: number;
}): WsHandler => {
  // TODO: Verify these arguments more aggressively.
  return async (args, accept, outboundHandler, _close) => {
    const name = args.name;
    const neosId = args.neosId;
    var ticketId: string | undefined = undefined;

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
                notifier
                  .invoke(
                    await ticket.setResponding({
                      name,
                      neosId,
                    })
                  )
                  .toMenteePayload(accept)
              );
            } else if (m.type === "complete") {
              outboundHandler(
                notifier
                  .invoke(await ticket.setCompleted())
                  .toMenteePayload(accept)
              );
            } else if (m.type === "unclaim") {
              outboundHandler(
                notifier
                  .invoke(await ticket.setRequested())
                  .toMenteePayload(accept)
              );
            }
          }
        }),
      onClose: () => {}, //notifier.unsubscribe(sub),
    };
  };
};
