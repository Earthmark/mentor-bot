import assert from "assert";
import { describe } from "mocha";

import { toObj, toStr } from "./req";

describe("Verify serializer writes as expected.", () => {
  it("returns formatted table when provided urlencoded.", () => {
    assert.deepStrictEqual(toObj("a=a&b=b"), {
      a: "a",
      b: "b",
    });
  });
});

describe("Verify deserializer reads as expected.", () => {
  it("returns formatted table when provided urlencoded.", () => {
    assert.deepStrictEqual(
      toStr({
        a: "a",
        b: "b",
      }),
      "a=a&b=b"
    );
  });
});
