import dotenv from "dotenv";

import { createDiscordStore, Ticket } from "./ticket";
import { SubscriptionNotifier } from "./subs";
import { logProm } from "./prom_catch";

dotenv.config();

logProm("Failure during startup")(async () => {
  // We first bind to the discord channel, if that fails there's no reason to continue (we don't have a DB).
  const store = await createDiscordStore(
    process.env.BOT_TOKEN ?? "",
    process.env.BOT_CHANNEL ?? ""
  );
  // The notifier bridges the websocket server and maintainer, sending notifications of updates.
  const notifier = new SubscriptionNotifier<Ticket>();

  console.log("Startup successful.");
});
