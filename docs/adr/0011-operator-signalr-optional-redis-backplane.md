# Operator SignalR: optional Redis backplane for scale-out

Operator Notifications and Feedback/Home live fan-out use ASP.NET Core SignalR with `Clients.User`. Without a backplane, each API process only sees its own connections — so multi-instance deploys silently miss toasts and classification badges. Scale-out is **opt-in via `ConnectionStrings:Redis`**: empty/missing keeps plain in-process SignalR (QA and early single-instance prod); a non-empty value wires the StackExchange Redis backplane for both hubs and **fail-fast pings Redis at startup** so a bad deploy does not run “half scale-out.” The same connection-string helper (`RedisConnection`) is the shared registration seam for later Redis uses (e.g. distributed cache); this ADR does not migrate `IMemoryCache` callers.

We rejected Azure SignalR Service for now (connection offload is not the bottleneck while instance count is low), requiring Redis in non-Dev (forces infra before need), and bundling cache migration into this slice. Multiple API instances **without** Redis remain unsupported.

Registration locality: `OperatorSignalRHubs.AddOperatorSignalR` next to the hub path registry (ADR-0009); JWT session rules unchanged. Classification durability stays ADR-0010 — Pending rows survive restarts regardless of SignalR topology.

Reopen if Azure SignalR Service becomes the preferred host, if Redis must be mandatory in an environment, or if a second backplane provider is required.
