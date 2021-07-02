import {
  Ticket,
  TicketStore,
  claimEmoji,
  unclaimEmoji,
  completeEmoji,
} from "./ticket";
import { Notifier } from "./channel";
import { log } from "./prom_catch";

// This observes added tickets for reactions, and advances the state machine if found.

// This is the oddest part of the code base, and may get refactored out.

const processRequestedErr = log("Error while processing a requested ticket");
const processUnclaimErr = log("Error while un-claiming a ticket");
const processRespondingErr = log("Error while replying to a responding ticket");

export const maintainDiscordLink = async (
  store: TicketStore,
  notifier: Notifier<Ticket>,
  historyLimit: number
): Promise<() => boolean> => {
  const observeTicket = (ticket: Ticket): void => {
    const ensureReload = <T>(handler: Promise<T>): Promise<T> =>
      handler.finally(() => observeTicket(notifier.invoke(ticket)));

    // Requested can go to Responding or Canceled (canceled is through the ws server).
    ticket.observeForReaction({
      [claimEmoji]: {
        mentorOnly: false,
        handler: (mentor) =>
          processRequestedErr(ensureReload(ticket.setResponding(mentor))),
      },
      [unclaimEmoji]: {
        mentorOnly: true,
        handler: () => processUnclaimErr(ensureReload(ticket.setRequested())),
      },
      [completeEmoji]: {
        mentorOnly: true,
        handler: () =>
          processRespondingErr(ensureReload(ticket.setCompleted())),
      },
    });
  };

  const healthCheck = store.observeTickets(observeTicket);

  // This currently has a race condition where an observation can happen over the same message twice,
  // if that message was added after the subscription starts but before the list grab starts.
  // Possibly add a cache of active maintains, and only add a new subscription of the previous only exists.
  // However that adds an object retention issue, where as the current retention is held by the event handler.
  await store.scanTickets(historyLimit, observeTicket);

  return healthCheck;
};
