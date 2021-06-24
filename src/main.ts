import dotenv from "dotenv";

import { SubscriptionNotifier } from "./subs";
import { logProm } from "./prom_catch";

dotenv.config();

logProm("Failure during startup")(async () => {
  const notifier = new SubscriptionNotifier();

  console.log("Startup successful.");
});
