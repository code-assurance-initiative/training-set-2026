# ParcelTracking.Client

Typed .NET client for the ParcelTracking API.

```csharp
services.AddParcelTrackingClient(new Uri("https://parcels.example.net/"))
    .AddHttpMessageHandler<MyAccessTokenHandler>()
    .AddStandardResilienceHandler();
```

The client does not choose a retry policy for you: registering a shipment is not idempotent, so whether and how to
retry it is the application's decision. Versioned with the service (Semantic Versioning); see the repository
CHANGELOG.
