# Personal data inventory and privacy notice

What personal data the club membership service holds, why, on what lawful basis (GDPR Art. 6), how it is protected
and how long it is kept. The controller is the club; this service is its system of record for memberships. Keep this
document in step with the code: a new field of personal data needs a row here before it is merged.

## Data classes

| Data                                              | Where                                     | Purpose                                                      | Lawful basis                 | Protection                                                           | Retention                                                      |
| ------------------------------------------------- | ----------------------------------------- | ------------------------------------------------------------ | ---------------------------- | -------------------------------------------------------------------- | -------------------------------------------------------------- |
| First and last name                               | `members.first_name`, `members.last_name` | Membership administration                                    | Contract                     | Storage encryption of the platform                                   | Until erasure; erased 24 months after the paid period ends     |
| E-mail address                                    | `members.email`                           | Log-in, uniqueness, reminders of booked classes              | Contract                     | Storage encryption of the platform (searchable, ADR 0004)            | As above                                                       |
| Phone number (optional)                           | `members.phone_encrypted`                 | SMS reminders, if the member consents                        | Consent (`sms-reminders`)    | Field-level AES-256-GCM (ADR 0004)                                   | As above                                                       |
| Date of birth                                     | `members.date_of_birth_encrypted`         | Minimum age (16)                                             | Contract                     | Field-level AES-256-GCM                                              | As above                                                       |
| Consents                                          | `member_consents`                         | Proof of what the member agreed to and when                  | Legal obligation (Art. 7(1)) | Storage encryption                                                   | As the member                                                  |
| Class bookings                                    | `class_bookings`                          | Running classes                                              | Contract                     | Storage encryption                                                   | As the member                                                  |
| Reminder deliveries (recipient address or number) | `reminder_deliveries`                     | Answering "did I get a reminder?" and provider disputes      | Legitimate interest          | Storage encryption                                                   | 90 days                                                        |
| Billing holder name and e-mail                    | `billing_accounts`                        | Invoicing                                                    | Contract                     | Storage encryption                                                   | Anonymised on erasure; invoices kept 5 years (bookkeeping law) |
| Billing date of birth                             | `billing_accounts.date_of_birth`          | Age-based concessions (student, senior)                      | Contract                     | Field-level AES-256-GCM                                              | Anonymised on erasure                                          |
| Audit log                                         | `audit_log`                               | Accountability: who changed which personal-data fields, when | Legitimate interest          | Holds field names, never values                                      | 5 years                                                        |
| Integration messages                              | `outbox_messages`                         | Telling Billing of registrations and erasures                | Contract                     | Deleted as soon as delivered                                         | Minutes                                                        |
| Application logs                                  | stdout → log store                        | Operations                                                   | Legitimate interest          | No personal data in clear: members appear as keyed pseudonyms (HMAC) | 30 days                                                        |

## Purposes that need consent

- `sms-reminders` — a text message ahead of a booked class. Optional; withdrawable at any time.
- `newsletter` — club news by e-mail.

Reminders of booked classes **by e-mail** are part of the service the member asked for by booking and need no consent.

## Data-subject rights

- **Access and portability** — `GET /api/members/:id/personal-data` returns everything Membership holds as JSON.
- **Rectification** — `PUT /api/members/:id/contact-details`.
- **Erasure** — `POST /api/members/:id/erasure` removes the member's personal data; Billing anonymises its copy when
  it receives the `membership.member-erased` message.
- **Withdrawing consent** — `PUT /api/members/:id/consents/:purpose` with `{"granted": false}`.

Data-subject requests need the `privacy.requests` scope, held by the club's privacy officer.
