
type STable = { [key: string]: string };

export function toObj(payload: string, accept?: string): STable {
  if (accept === "application/json") {
    return JSON.parse(payload);
  } else {
    return Object.fromEntries(new URLSearchParams(payload).entries());
  }
}

export function toStr(payload: STable, accept?: string) {
  if (accept === "application/json") {
    return JSON.stringify(payload);
  } else {
    return new URLSearchParams(Object.entries(payload)).toString();
  }
}