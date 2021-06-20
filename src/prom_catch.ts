export const log =
  (ctx: string) =>
  <T>(p: Promise<T>): void =>
    void p.catch((e) => console.log(ctx, e));
