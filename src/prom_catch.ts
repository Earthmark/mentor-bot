export const log =
  (ctx: string) =>
  (p: Promise<void>): void =>
    void p.catch((e) => console.log(ctx, e));
