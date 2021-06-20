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

  start = async (): Promise<void> => {
    this.#store.observeTickets(this.#observeTicket);

    // This currently has a race condition, possibly add a debounce here.
    return this.#store.scanTickets(30, this.#observeTicket);
  };

  #observeTicket = (ticket: Ticket): void => {
    switch (ticket.getStatus()) {
      case "requested":
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

  #recordUpdateAndReload = (ticket: Ticket): void => {
    this.#observeTicket(this.#notifier.invoke(ticket));
  };
}
