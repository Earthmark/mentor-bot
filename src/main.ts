import dotenv from "dotenv";

import { createDiscordStore, Ticket } from "./ticket.js";
import { maintainDiscordLink } from "./maintainer.js";
import { createWsServer } from "./mentee_ws_handler.js";
import { createChannel } from "./channel.js";
import { createServer } from "./httpServer.js";
import { logProm } from "./prom_catch.js";

dotenv.config();

logProm("Failure during startup")(async () => {
  // We first bind to the discord channel, if that fails there's no reason to continue (we don't have a DB).
  const store = await createDiscordStore(
    process.env.BOT_TOKEN ?? "",
    process.env.BOT_CHANNEL ?? ""
  );
  // The notifier bridges the websocket server and maintainer, sending notifications of updates.
  const notifier = createChannel<Ticket>();

  // This creates the handler used to process web sockets from the client.
  const menteeHandler = createWsServer({
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
    menteeHandler,
    mentorHandler: menteeHandler,
  });

  console.log("Startup successful.");
});
