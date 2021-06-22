import dotenv from "dotenv";

import { createDiscordStore } from "./ticket";
import { maintainDiscordLink } from "./maintainer";
import { createWsServer } from "./wsServer";
import { SubscriptionNotifier } from "./subs";
import { createServer } from "./httpServer";
import { logProm } from "./prom_catch";

dotenv.config();

logProm("Failure during startup")(async () => {
  const store = await createDiscordStore(process.env.BOT_TOKEN ?? "");
  const notifier = new SubscriptionNotifier();

  const wsHandler = createWsServer({
    pingMs: parseInt(process.env.PING_RATE_MS ?? "25000", 10),
    stopDelay: parseInt(process.env.STOP_SIGNAL_DELAY_MS ?? "10000", 10),
    store,
    notifier,
  });

  const maintainer = await maintainDiscordLink(
    store,
    notifier,
    parseInt(process.env.HISTORY_LIMIT ?? "30", 10)
  );

  createServer({
    port: parseInt(process.env.PORT ?? "8080", 10),
    healthChecks: [maintainer],
    wsHandler: wsHandler,
  });

  console.log("Initialization successful.");
});
