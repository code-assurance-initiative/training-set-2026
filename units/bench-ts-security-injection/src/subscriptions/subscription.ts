import type { Collection } from 'mongodb';

export interface Subscription {
  readonly _id: string;
  readonly reportId: string;
  readonly email: string;
  /** Random; sent in every report mail so the recipient can unsubscribe without signing in. */
  readonly unsubscribeToken: string;
  readonly createdAt: Date;
}

export type SubscriptionCollection = Pick<
  Collection<Subscription>,
  'findOne' | 'find' | 'insertOne' | 'deleteOne'
>;
