import type { ClassBookingRepository } from './classes/class-booking-repository.js';
import type { ClassSessionRepository } from './classes/class-session-repository.js';
import type { MemberRepository } from './members/member-repository.js';

/** The repositories of one database transaction of the Membership context. */
export interface MembershipRepositories {
  readonly members: MemberRepository;
  readonly sessions: ClassSessionRepository;
  readonly bookings: ClassBookingRepository;
}

export interface MembershipTransactions {
  /** Runs `work` in one transaction: everything it saves commits together or not at all. */
  run<T>(work: (repositories: MembershipRepositories) => Promise<T>): Promise<T>;
}
