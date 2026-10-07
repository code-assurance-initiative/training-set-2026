# Consumed event schemas

Pinned copies of the producers' schemas that the notifier implements (`src/Quellbrook.Notifier/Messaging/Messages.cs`):

| Schema | Producer | Version pinned | Fields the notifier reads |
|---|---|---|---|
| `orders/order-placed.v1.schema.json` | estate-quellbrook-orders | v0.3.0 | order id, consignee name, contact e-mail and phone |
| `dispatch/consignment-out-for-delivery.v1.schema.json` | estate-quellbrook-dispatch | v0.3.0 | order id |
| `dispatch/consignment-delivered.v1.schema.json` | estate-quellbrook-dispatch | v0.3.0 | order id, delivery time |

Within v1 the producers only add optional fields; the notifier ignores fields it does not read. Update a copy when its
producer announces a new version.
