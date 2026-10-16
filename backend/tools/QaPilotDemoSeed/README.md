# QaPilotDemoSeed

Ops CLI: seed Location Guests and succeeded Feedback onto an existing Pilot restaurant resolved by owner email.

## Product demo (`/product-demo`)

The public walkthrough uses `POST /api/auth/demo-session`, which signs in the fixed owner from config:

- `DemoSession:Enabled`
- `DemoSession:OwnerEmail`

Development `DemoSession:OwnerEmail` must match an **existing** Single Pilot owner in the local DB (see `appsettings.Development.json`).

### One-time setup

1. Pick a **Single** Pilot restaurant owner email that already exists (signup or sync).
2. Set `DemoSession:OwnerEmail` (and `DemoSession:Enabled=true`) to that email.
3. Optionally seed guests/feedback:

```bash
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=TummlyDB;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=True;'
dotnet run --project backend/tools/QaPilotDemoSeed -- \
  --email YOUR_OWNER@example.com \
  --replace \
  --guests 20 \
  --feedback 36
```

4. Restart the API after changing `DemoSession` config.

Acceptance: Feedback and Guests list endpoints return rows for that owner JWT.
