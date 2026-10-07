# @parcel-tracking/webhooks

Verify and parse parcel-tracking webhooks in a merchant's Node.js service.

```ts
import { parseWebhookEvent, verifyWebhookSignature } from "@parcel-tracking/webhooks";

const check = verifyWebhookSignature({
  secret: process.env.PARCEL_WEBHOOK_SECRET!,
  header: req.get("parcel-signature") ?? "",
  body: rawBody,
});
if (!check.valid) return res.status(400).end();
const event = parseWebhookEvent(rawBody);
```

Verify the **raw** body: a re-serialised JSON object does not have the bytes that were signed. Signatures older than
five minutes are rejected by default (`toleranceSeconds`).
