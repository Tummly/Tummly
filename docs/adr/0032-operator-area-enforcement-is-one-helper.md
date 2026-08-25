# Operator Area enforcement is one helper

Hide-nav and SPA route guards are UX only. Every Operator HTTP action, export, Assistant call, and location-shaped SignalR payload must call one restaurant permission helper that loads **Active** membership on the request (JWT is identity). Identified-but-denied is **403**, not **404** (404 is missing row or unknown location id after the helper passes). The Assistant is not a backdoor: each retrieve and write still uses the source and target **Area** cell plus **Location scope**. Account controls guest-data export needs `account-workspace` **Manage**, not **View**.

We rejected per-controller copies of the matrix, freezing Area levels in the JWT, and using **404** to hide **No access** pages. Product lock: `.scratch/team-and-permissions/issues/08-server-side-enforcement-on-apis-exports-and-ai.md`.
