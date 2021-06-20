import {
  Ticket,
  TicketStore,
  claimEmoji,
  unclaimEmoji,
  completeEmoji,
} from "./ticket";
import { SubscriptionNotifier } from "./subs";
import { log } from "./prom_catch";

const processRequestedErr = log("Error while processing a requested ticket");
const processUnclaimErr = log("Error while unclaiming a ticket");
const processRespondingErr = log("Error while replying to a responding ticket");

export class DiscordMaintainer {
  #store: TicketStore;
  #notifier: SubscriptionNotifier;
  constructor(store: TicketStore, notifer: SubscriptionNotifier) {
    this.#store = store;
    this.#notifier = notifer;
  }

  // This needs to eventually subscribe to the notifier,
  // as observations are not removed on ticket cancel (which happens via the ws).

  // This observes messages in the store for reactions,
  // moving the tickets through the reaction based state machine.
  start = async (historyLimit: number): Promise<void> => {
    this.#store.observeTickets(this.#observeTicket);

    // This currently has a race condition where an observation can happen over the same message twice,
    // if that message was added after the subscription starts but before the list grab starts.
    // Possibly add a cache of active maintains, and only add a new subscription of the previous only exists.
    // However that adds an object retention issue, where as the current retention is held by the event hander.
    return this.#store.scanTickets(historyLimit, this.#observeTicket);
  };

  // This sets up the observers for a particular ticket.
  #observeTicket = (ticket: Ticket): void => {
    switch (ticket.getStatus()) {
      case "requested":
        // Requested can go to Responding or Canceled (canceled is through the ws server).
        ticket.observeForReaction(
          {
            [claimEmoji]: (mentor) =>
              processRequestedErr(
                ticket
                  .setResponding(mentor)
                  .finally(() => this.#recordUpdateAndReload(ticket))
              ),
          },
          /*mentor_only=*/ false
        );
        break;
      case "responding":
        // Responding can go to requested or complete (or canceled, but that's through the ws server).
        ticket.observeForReaction(
          {
            [unclaimEmoji]: () =>
              processUnclaimErr(
                ticket
                  .setRequested()
                  .finally(() => this.#recordUpdateAndReload(ticket))
              ),
            [completeEmoji]: () =>
              processRespondingErr(
                ticket
                  .setCompleted()
                  .finally(() => this.#recordUpdateAndReload(ticket))
              ),
          },
          /*mentor_only=*/ true
        );
        break;
    }
  };

  // Alerts the notifier and requeues for observation.
  #recordUpdateAndReload = (ticket: Ticket): void => {
    this.#observeTicket(this.#notifier.invoke(ticket));
  };
}
