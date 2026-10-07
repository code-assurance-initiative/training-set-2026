import { Router } from 'express';
import type { Services } from '../composition.js';
import { bookClassRoutes } from './features/book-class/book-class.js';
import { cancelMembershipRoutes } from './features/cancel-membership/cancel-membership.js';
import { changeContactDetailsRoutes } from './features/change-contact-details/change-contact-details.js';
import { eraseMemberRoutes } from './features/erase-member/erase-member.js';
import { exportMemberDataRoutes } from './features/export-member-data/export-member-data.js';
import { recordConsentRoutes } from './features/record-consent/record-consent.js';
import { registerMemberRoutes } from './features/register-member/register-member.js';
import { scheduleClassRoutes } from './features/schedule-class/schedule-class.js';

export function membershipRoutes(services: Services): Router {
  const slices = services.membership;
  return Router().use(
    registerMemberRoutes(slices.register),
    changeContactDetailsRoutes(slices.changeContactDetails),
    recordConsentRoutes(slices.recordConsent),
    scheduleClassRoutes(slices.scheduleClass),
    bookClassRoutes(slices.bookClass, slices.upcoming, services.clock),
    cancelMembershipRoutes(slices.cancelMembership),
    eraseMemberRoutes(slices.eraseMember),
    exportMemberDataRoutes(slices.exportMemberData),
  );
}
