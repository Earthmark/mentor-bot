import assert from "assert";
import { describe } from "mocha";
import Discord from "discord.js";
import jest from "jest";

import createChan from "./channel";
import discordStore, { TicketStore, Ticket } from "./ticket";

const createMockStore = (): Promise<TicketStore> => {
  var id = 0;
  const messages: Record<string, Discord.Message> = {};
  const chan = createChan<Ticket>();
  const store = discordStore({
    token: "t",
    channel: "c",
    notifier: chan,
    client: {
      login: () => Promise.resolve(""),
      channels: {
        fetch: async () =>
          ({
            isText: () => true,
            send: async (
              embed: Discord.MessageEmbed
            ): Promise<Discord.Message> => {
              const msg = {
                id: id++ + "",
                embeds: [embed],
                react: async () => ({}),
              } as unknown as Discord.Message;
              return (messages[msg.id] = msg);
            },
            messages: {
              fetch: async (msg: string) => {
                if (msg in messages) {
                  return messages[msg];
                }
                throw new Error("Not found");
              },
            },
          } as unknown as Discord.TextChannel),
      } as unknown as Discord.ChannelManager,
    } as unknown as Discord.Client,
  });

  return store;
};

describe("ticket store returns expected tickets.", () => {
  it("Mentee encoded returns expected payload (not including mentor info).", async () => {
    const store = await createMockStore();
    const ticket = await store.createTicket({
      name: "Taco",
      session: "Truck",
    });
    assert.strictEqual(ticket.toMenteePayload(""), "ticket=0&status=requested");
  });
});
