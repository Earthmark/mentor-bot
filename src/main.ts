import dotenv from "dotenv";

import { createDiscordStore, Ticket } from "./ticket";
import { maintainDiscordLink } from "./maintainer";
import createMenteeHandler from "./mentee_ws_handler";
import createMentorHandler from "./mentor_ws_handler";
import { createChannel } from "./channel";
import { createServer } from "./httpServer";
import { logProm } from "./prom_catch";

dotenv.config();

logProm("Failure during startup")(async () => {
  // The notifier bridges the websocket server and maintainer, sending notifications of updates.
  const channel = createChannel<Ticket>();

  // We first bind to the discord channel, if that fails there's no reason to continue (we don't have a DB).
  const store = await createDiscordStore(
    process.env.BOT_TOKEN ?? "",
    process.env.BOT_CHANNEL ?? "",
    channel
  );

  // This creates the handler used to process web sockets from the client.
  const menteeHandler = createMenteeHandler({
    stopDelay: parseInt(process.env.STOP_SIGNAL_DELAY_MS ?? "10000", 10),
    store,
    subscriber: channel,
  });

  const mentorHandler = createMentorHandler({
    store,
    subscriber: channel,
  });

  // This creates a listener to the discord channel,
  // we need a health check here as this can die independently of the http server.
  const maintainer = await maintainDiscordLink(
    store,
    parseInt(process.env.HISTORY_LIMIT ?? "30", 10)
  );

  // This creates and binds the http server.
  createServer({
    port: parseInt(process.env.PORT ?? "8080", 10),
    healthChecks: [maintainer],
    menteeHandler,
    mentorHandler,
  });

  console.log("Startup successful.");
});
