# Operator Notifications SignalR uses JWT bearer, not cookies

Day-one Operator Notifications realtime (badge + toast) uses an ASP.NET SignalR hub authorized with the same **JWT bearer** scheme as operator REST APIs: the JS client sends the token via `accessTokenFactory` (hub `access_token` query for WebSockets/SSE), the hub is `[Authorize]`, and fan-out targets `Clients.User` with `ClaimTypes.NameIdentifier`. Connection lifetime is the signed-in Operator dashboard shell visit; reconnect uses automatic reconnect plus **REST inbox/badge catch-up** because SignalR does not replay missed messages. Prefer closing the connection when the JWT expires (60-minute tokens, no refresh flow today).

We rejected cookie auth for the hub: the operator session is already a localStorage JWT attached as `Authorization: Bearer` on REST, and introducing cookie auth for SignalR alone would split session models. Research and product rules: `.scratch/operator-notifications/assets/02-signalr-fit.md` and `.scratch/operator-notifications/assets/decision-package.md`.

**Shared session (hubs stay separate):** Operator Notifications and Feedback/Home each keep their own hub and connection lifetime (shell vs Home). JWT query-token allowlisting, `CloseOnAuthenticationExpiration`, and the JS client reconnect/stop-when-token-gone policy are shared: backend `OperatorSignalRHubs` registry + `MapOperatorHub`, frontend `connectJwtSignalRSession` / `operatorHubUrl`. A third operator hub adds one registry path and a thin domain `connect*Hub` binder — it does not merge hubs or change auth.

Reopen if operator auth moves to cookie/BFF sessions, if a refresh-token flow changes connection lifetime, or if Notifications must share a hub with other realtime surfaces under different auth rules.
