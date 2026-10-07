import { Router } from 'express';
import type { ReservationService } from '../../application/reservations/reservation-service.js';
import { createFromBody, withParam } from '../handlers.js';
import { createReservationBody, reservationIdParam } from '../schemas.js';
import { requireScope, Scopes } from '../scopes.js';

export function reservationRoutes(reservations: ReservationService): Router {
  const write = requireScope(Scopes.reservationsWrite);

  return Router()
    .get(
      '/reservations/:id',
      requireScope(Scopes.stockRead),
      withParam('id', reservationIdParam, (id) => reservations.get(id)),
    )
    .post(
      '/reservations',
      write,
      createFromBody(
        createReservationBody,
        (request) => reservations.create(request),
        (reservation) => `/api/reservations/${reservation.id}`,
      ),
    )
    .post(
      '/reservations/:id/release',
      write,
      withParam('id', reservationIdParam, (id) => reservations.release(id)),
    )
    .post(
      '/reservations/:id/fulfilment',
      write,
      withParam('id', reservationIdParam, (id) => reservations.fulfil(id)),
    );
}
