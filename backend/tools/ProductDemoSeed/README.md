# ProductDemoSeed — Mavero Kitchen

Seeds the public `/product-demo` account: **Mavero Kitchen**, location **Camden High Street**, Pilot credits, Capture/Guests/Feedback/Recovery/Offers/Campaigns/Weekly Brief, brand logo, and AI usage.

Owner email (synthetic, not for login UI): `mavero.demo@qa-seed.tummly.invalid`

When the stakeholder supplies a real dedicated mailbox, change only:

- `DemoSeedConstants.OwnerEmail` (re-seed), and
- `DemoSession:OwnerEmail` / `DemoSession__OwnerEmail`

Do **not** point DemoSession at personal staff accounts.

## One command

```bash
# From repo root — loads MSSQL_SA_PASSWORD from backend/TummlyDb/vps/.env when
# ConnectionStrings__DefaultConnection is unset (Docker tummly-mssql on :1433).
dotnet run --project backend/tools/ProductDemoSeed -- --replace
```

```powershell
dotnet run --project backend/tools/ProductDemoSeed -- --replace
```

Flags:

- `--replace` — wipe prior seed rows for this restaurant only, then re-seed
- `--dry-run` — print plan, no writes

Apply API migrations first (includes `QrFormOpenEvents` + demo session overlay tables):

```bash
dotnet ef database update --project backend/TummlyBackend
```

## Wire DemoSession

```json
"DemoSession": {
  "Enabled": true,
  "OwnerEmail": "mavero.demo@qa-seed.tummly.invalid",
  "SuppressSideEffects": true
}
```

Restart the API after config changes.

## Brand logo

Asset: `Assets/mavero-kitchen-logo.jpg` (approved red Mavero mark). Seed sets `Restaurant.BrandLogoObjectKey=brand-logos/mavero-kitchen-demo.jpg` and uploads when `ObjectStorage__*` is configured. Without object storage, the key is still set — upload will warn until storage is available.

## Sandbox (§15 / Track F)

- Demo JWT includes `demo_session=true` and unique `demo_session_id`
- Contaminating mutations (workflow, guest-response send, redeem, campaign fire) write to `DemoSessionOverlays` only — baseline seed unchanged
- Payment / shop order writes return 403
- Email/SMS to synthetic destinations suppressed
- Overlay rows expire (default 8h); background purge every 15 minutes
- Ops `--replace` refreshes the canonical baseline

## Acceptance smoke (§16)

1. `POST /api/auth/demo-session` → Mavero owner JWT with `demo_session_id`
2. Settings/Home: Mavero Kitchen, Camden High Street, brand logo when storage is up
3. Feedback: Grace / Walk-in 1141; Needs attention ≈ 44 in Last 30
4. Guests: Maya Collins; ~302 guests; marketing-eligible present
5. Capture: four Active placements; scans ~1842; form opens ~1516
6. Offers/Campaigns non-zero; Weekly Briefs ×4 including `monday:2026-09-28`
7. AI usage ~64 consumptions on the AI channel
8. Two parallel demo sessions: mutate in A; B still sees canonical seed
9. AI Assistant (§13): ask about Grace Turner dry chicken, Maya Collins, Needs attention, Tuesday Table Boost — answers should cite seeded rows; note any tool gaps in the PR

Post-seed totals from a successful `--replace` (local Docker, 2026-10-16): NeedsAttention 44, recoveries 38/31, form opens 1516, WeeklyBriefs 4, AI 64.

## Staff redemption (optional)

Staff user: `mavero.staff@qa-seed.tummly.invalid` (synthetic; not for public login UI).
