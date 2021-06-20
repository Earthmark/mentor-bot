import dotenv from "dotenv";

import { createDiscordStore } from "./ticket";
import { DiscordMaintainer } from "./maintainer";
import { WsServer } from "./server";
import { SubscriptionNotifier } from "./subs";

dotenv.config();

createDiscordStore(process.env.BOT_TOKEN ?? "")
  .then(async (store) => {
    const notifier = new SubscriptionNotifier();
    new WsServer({
      port: parseInt(process.env.PORT ?? "8080", 10),
      pingMs: parseInt(process.env.PING_RATE_MS ?? "25000", 10),
      stopDelay: parseInt(process.env.STOP_SIGNAL_DELAY_MS ?? "10000", 10),
      store,
      notifier,
    });
    await new DiscordMaintainer(store, notifier).start(
      parseInt(process.env.HISTORY_LIMIT ?? "30", 10)
    );
    console.log("Initialization successful.");
  })
  .catch((e) => console.log("Failure during startup", e));
