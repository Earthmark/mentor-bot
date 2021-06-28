import dotenv from "dotenv";

import { createDiscordStore, Ticket } from "./ticket";
import { maintainDiscordLink } from "./maintainer";
import { createWsServer } from "./wsServer";
import { SubscriptionNotifier } from "./subs";
import { createServer } from "./httpServer";
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

  // This creates the handler used to process web sockets from the client.
  const wsHandler = createWsServer({
    pingMs: parseInt(process.env.PING_RATE_MS ?? "25000", 10),
    stopDelay: parseInt(process.env.STOP_SIGNAL_DELAY_MS ?? "10000", 10),
    store,
    notifier,
  });

  // This creates a listener to the discord channel,
  // we need a health check here as this can die independently of the http server.
  const maintainer = await maintainDiscordLink(
    store,
    notifier,
    parseInt(process.env.HISTORY_LIMIT ?? "30", 10)
  );

  // This creates and binds the http server.
  createServer({
    port: parseInt(process.env.PORT ?? "8080", 10),
    healthChecks: [maintainer],
    wsHandler: wsHandler,
  });

  console.log("Startup successful.");
});
