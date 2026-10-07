# @parcel-tracking/client

Typed client for the parcel-tracking merchant API.

```ts
import { TrackingClient } from "@parcel-tracking/client";

const client = new TrackingClient({
  baseUrl: "https://tracking.example.com/",
  token: () => tokens.current(),
});

const parcel = await client.getParcel("NPX12345678", { signal: AbortSignal.timeout(5_000) });
console.log(parcel.status, parcel.events.length);
```

The client sets no timeout and does not retry: pass an `AbortSignal` per call and wrap calls in the retry policy your
service already uses. A non-2xx answer throws `TrackingApiError` with the problem's `status`, `title` and `detail`.

Requires Node.js 20.19 or later (ESM only).
