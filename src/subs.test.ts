import assert from "assert";
import { describe } from "mocha";

import { SubscriptionNotifier } from "./subs";
import { Ticket } from "./ticket";

const id = "1";
const message: Ticket = {
  id: id,
} as Ticket;

describe("When a subscriber is notified.", () => {
  it("Then it will be invoked.", (done) => {
    const notifer = new SubscriptionNotifier<Ticket>();
    notifer.subscribe(id, () => done());
    notifer.invoke(message);
  });

  it("Then it will be invoked for every subscription.", () => {
    const notifer = new SubscriptionNotifier();
    // this is a counter using prime multiples to merge two different counters together.
    // This is asserted as expected = c1 * c2, as c1 and c2 will be different primes.
    let invokeCount = 1;
    notifer.subscribe(id, () => (invokeCount *= 2));
    notifer.subscribe(id, () => (invokeCount *= 3));
    notifer.invoke(message);
    assert.strictEqual(invokeCount, 6);
  });

  it("Then it will not be invoked after unsubscribed.", () => {
    const notifer = new SubscriptionNotifier();
    const sub = notifer.subscribe(id, () =>
      assert.fail("Expected unsubscription to actually unsubscribe.")
    );
    notifer.unsubscribe(sub);
    notifer.invoke(message);
  });

  it("Then an unsubscription does not affect the other subscription.", (done) => {
    const notifer = new SubscriptionNotifier();
    const sub = notifer.subscribe(id, () =>
      assert.fail("Unexpected subscription was invoked.")
    );
    notifer.subscribe(id, () => done());
    notifer.unsubscribe(sub);
    notifer.invoke(message);
  });

  it("Then an unsubscription does not affect the other subscription, but reverse subscription order.", (done) => {
    const notifer = new SubscriptionNotifier();
    const sub = notifer.subscribe(id, () =>
      assert.fail("Unexpected subscription was invoked.")
    );
    notifer.subscribe(id, () => done());
    notifer.unsubscribe(sub);
    notifer.invoke(message);
  });

  it("Then an unsubscription does not affect the other subscription, but reverse unsubscription rate.", (done) => {
    const notifer = new SubscriptionNotifier();
    notifer.subscribe(id, () => done());
    const sub = notifer.subscribe(id, () => {
      assert.fail("Unexpected subscription was invoked.");
    });
    notifer.unsubscribe(sub);
    notifer.invoke(message);
  });

  it("Then resubscription allows notification", (done) => {
    const notifer = new SubscriptionNotifier();
    notifer.unsubscribe(
      notifer.subscribe(id, () => {
        assert.fail("Unexpected subscription was invoked.");
      })
    );
    notifer.subscribe(id, () => done());
    notifer.invoke(message);
  });
});
