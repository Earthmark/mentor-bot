type STable = { [key: string]: string };

// Converts from a string to a map of strings, based on the recieved format.
export const toObj = (payload: string, accept?: string): STable => {
  if (accept === "application/json") {
    return JSON.parse(payload) as STable;
  } else {
    return Object.fromEntries(new URLSearchParams(payload).entries());
  }
};

// Converts from a map of strings into a string, encoded based on received format.
export const toStr = (payload: STable, accept?: string): string => {
  if (accept === "application/json") {
    return JSON.stringify(payload);
  } else {
    return new URLSearchParams(Object.entries(payload)).toString();
  }
};
