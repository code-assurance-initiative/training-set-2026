# Personal data in the notifier

| Data | Why it is held | Where | Retention |
|---|---|---|---|
| Consignee name, e-mail address and phone number (as announced with the order) | to notify the consignee about this delivery | `recipients` | 30 days after the delivery; 60 days after the announcement when the order is never delivered |
| What was sent, when and through which channel, with the recipient masked (`m***@post.example`, `+45***50`) | to answer "did you notify me?" and to send nothing twice | `notification_log` | 90 days |
| Message ids of processed events | to process each event once | `processed_messages` | 30 days |

The retention periods are enforced by `RetentionSweeper` every hour (`Retention__*` settings). Data is encrypted in transit (TLS to the database, broker and
providers) and at rest by the managed database; the notifier adds no field-level encryption. There is no on-request
erasure operation: a request goes to customer service, and the contact details are gone 30 days after the delivery at
the latest.

The e-mail provider and the SMS gateway receive the address or number of each message they deliver; they are
processors under the platform's data-processing agreements.
