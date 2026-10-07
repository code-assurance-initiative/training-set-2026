# Architecture — operator console

```mermaid
C4Container
  title Containers — operator console
  Person(operator, "Operator", "Customer service and dispatch staff")
  Container_Ext(proxy, "Ingress + authentication proxy", "ingress-nginx, oauth2-proxy", "TLS, sign-in, session cookie")
  Container(web, "Console", "React 19, Vite, served by nginx", "Orders, shipment status, dispatch board")
  Container_Ext(gateway, "API gateway / BFF", "estate-quellbrook-gateway")
  Rel(operator, proxy, "https://ops.quellbrook.example", "HTTPS")
  Rel(proxy, web, "/ (static files)", "HTTP over mesh mTLS")
  Rel(proxy, gateway, "/api", "HTTP over mesh mTLS")
  Rel(web, gateway, "fetch /api (same origin, session cookie)", "HTTPS via the proxy")
```

| Folder                             | Responsibility                                                                        |
| ---------------------------------- | ------------------------------------------------------------------------------------- |
| `src/api/`                         | the gateway client (`request`, problem details as `ApiError`) and one module per area |
| `src/features/`                    | one folder per screen                                                                 |
| `src/components/`                  | layout, links, status badge, error message                                            |
| `src/routing.ts`, `src/useLoad.ts` | path routing with the History API; loading with cancellation                          |
| `nginx/`                           | the server configuration with the security headers                                    |

The console stores no token (ADR 0002).
