# 5. Reminders are sent from an outbox by a background worker

Date: 2026-03-10
Status: Accepted

## Context

Patients get an SMS reminder two days before and on the morning of an appointment. Sending from inside the booking
request would make booking slow and fragile (the SMS gateway is slow and sometimes down), and a moved or cancelled
appointment must not trigger reminders for its old time.

## Decision

When an appointment is booked or rescheduled, the application plans its reminders (`ReminderPlanner`) and stores them
in a reminder outbox (`IReminderOutbox`). Cancelling or rescheduling withdraws the appointment's unsent reminders. A
hosted service (`ReminderDispatchWorker`) polls the outbox and sends the reminders that are due. No HTTP request sends
a message itself.

## Consequences

- Booking does not depend on the SMS gateway being available.
- A reminder is sent at most once per planned entry; a failed batch is retried on the next poll.
- The outbox is in memory until persistence is introduced, so planned reminders are lost on restart (see README).
