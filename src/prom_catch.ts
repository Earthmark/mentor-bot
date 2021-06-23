// This creates a logger to record a promise error, without having the runtime handle it itself.
export const log =
  (ctx: string) =>
  <T>(p: Promise<T>): void =>
    void p.catch((e) => console.log(ctx, e));

// This creates a logger that instantly calls the provided function, suppressing any observed errors.
export const logProm =
  (ctx: string) =>
  <T>(p: () => Promise<T>): void =>
    void p().catch((e) => console.log(ctx, e));
