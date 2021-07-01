// Converts from a string to a map of strings, based on the received format.
export const toObj = (
  payload: string,
  accept?: string
): Record<string, string> => {
  if (accept === "application/json") {
    return JSON.parse(payload) as Record<string, string>;
  } else {
    return Object.fromEntries(new URLSearchParams(payload).entries());
  }
};

// Converts from a map of strings into a string, encoded based on received format.
export const toStr = (
  payload: Record<string, string>,
  accept?: string
): string => {
  if (accept === "application/json") {
    return JSON.stringify(payload);
  } else {
    return new URLSearchParams(Object.entries(payload)).toString();
  }
};
