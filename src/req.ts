type STable = { [key: string]: string };

export const toObj = (payload: string, accept?: string): STable => {
  if (accept === "application/json") {
    return JSON.parse(payload) as STable;
  } else {
    return Object.fromEntries(new URLSearchParams(payload).entries());
  }
};

export const toStr = (payload: STable, accept?: string): string => {
  if (accept === "application/json") {
    return JSON.stringify(payload);
  } else {
    return new URLSearchParams(Object.entries(payload)).toString();
  }
};
