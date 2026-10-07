/** The source of the current time, injected so that expiry rules can be tested without waiting. */
export interface Clock {
  now(): Date;
}

export const systemClock: Clock = {
  now: () => new Date(),
};
