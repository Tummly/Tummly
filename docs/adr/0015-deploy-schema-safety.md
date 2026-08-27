# Deploy schema safety for QA and Prod API

A bad backend push must not take traffic on QA or Prod when schema/migrations are incomplete or migrate fails. We keep **`MigrateAsync` on startup** and fail closed: CI blocks incomplete EF migrations, `/health/ready` stays **503** until DB init and schema are ready, the process **exits** after migrate retries are exhausted, and Azure Container Apps **Single** revision mode plus an HTTP readiness probe keep the last healthy revision serving. **QA and Prod use the same policy bar.**

This ADR defines the policy. Implementation landed on the same branch (see **Implementation** below).

## CI gates (required)

Against `backend/TummlyBackend` (EF Core 8+), CI must fail the PR when either check fails:

1. **Migration ↔ Designer pairing** — every `Migrations/*_*.cs` (excluding `*Designer.cs` / `*ModelSnapshot.cs`) has a matching `*.Designer.cs`, and vice versa. Pairing alone catches “ghost” migrations (Up committed, Designer missing) that `dotnet build` and pending-model checks still pass, because EF skips attribute-less `Migration` types and never runs `Up`. Generate with `dotnet ef migrations add`; do not hand-author Designers.
2. **`dotnet ef migrations has-pending-model-changes`** — non-zero when the current model ≠ `ApplicationDbContextModelSnapshot` (forgotten migrations / snapshot drift).
3. **Restaurants → Users SET NULL ban** — `scripts/check-restaurant-user-setnull-fks.sh` fails when a migration `Up` adds `FK` from `Restaurants` to `Users` with `ReferentialAction.SetNull` (SQL Server **1785** with `OwnerUserId` CASCADE). Prefer `NoAction` / `Restrict`.

**Recommended (optional):** reconcile `dotnet ef migrations list --no-connect` ids with migration class filenames (same class of undiscoverable migration as pairing). Full migrate dry-run against SQL Server remains deferred (see Deferred).

## `/health/ready` contract

`GET /health/ready` returns **200** only when **all** hold:

1. **Can connect** to the database (`CanConnectAsync` or equivalent).
2. **DB init-complete** — startup DB initialization finished successfully. When `Database:ApplyMigrationsOnStartup` is enabled, that includes a successful `MigrateAsync`. Seed Admin/Support is **best-effort** and does **not** gate ready.
3. **No pending EF migrations** — even if `ApplyMigrationsOnStartup` is **false**. A schema-behind app must not take traffic.

Otherwise **503**, including while init is **in progress** and after init **failed** (ready stays 503 until the process hard-exits).

**v1 non-goal:** no deep model-vs-database / column-level schema validation in ready. CI + pending-migration check + successful migrate cover that class of bug for v1.

`GET /health` remains a lightweight liveness signal (process up); it must **not** require migrate/schema readiness.

## Hard exit after migrate failure

When `ApplyMigrationsOnStartup` is enabled and migrate retries are exhausted, the process **must exit** (non-zero). Do not leave a long-lived instance that logs migrate failure and continues serving (or sitting at permanent 503 without recycling). Combined with ACA readiness, the new revision never becomes ready for traffic.

Retry budget today is on the order of **30 × 5s (~150s)**; probe settings below must cover at least that window (or whatever retry budget the implementation keeps).

## Azure Container Apps

For the API Container App in **QA and Prod**:

1. **`activeRevisionsMode: Single`** — required so the previous healthy revision keeps **100%** of ingress until the new revision is ready. Do not rely on Multiple mode for this safety property.
2. **HTTP readiness probe** on **`/health/ready`** (ingress target port). Success = HTTP 200–399; **503 = not ready** → no traffic shift. Budget must be **≥ migrate retry window**. With custom-probe `failureThreshold` max **10**, use a long period rather than copying portal TCP defaults — e.g. `initialDelaySeconds: 10`, `periodSeconds: 20`, `failureThreshold: 10` (~200s). A matching **startup** probe with the same long budget is recommended.
3. **Liveness (optional):** HTTP **`/health` only** — never fail-closed `/health/ready`, so migrate-time 503s do not restart the container mid-init.

Default TCP probes (port open only) are **not** sufficient for this policy.

## Traffic and operator notice

- **Expected outcome of a bad schema/migrate push:** last healthy revision keeps serving; the new revision never takes traffic (failed activation / unhealthy revision).
- **GitHub Actions green ≠ traffic cutover.** CI can pass and the Azure revision can still fail ready or crash on migrate. Do not treat a green workflow as proof the new revision is live.
- **How we notice (v1):** Azure revision health, Container App logs, and the site still answering on the old revision. **No new paging/alerting** in this ADR.
- **Stuck revision diagnosis (agents):** when a **new** API path returns empty 404 while a **known-old** path returns 401, treat it as a *stuck revision* first — see [docs/agents/stuck-revision.md](../agents/stuck-revision.md) and `scripts/probe-qa-api-revision.sh`.

## Deferred (explicit)

- Separate migrate job (out of process from the API container).
- Deep model-vs-database validation beyond pending migrations + successful migrate.
- Full migrate dry-run against SQL Server in CI (Testcontainers); v1 uses the Restaurants→Users SET NULL static ban plus Ready fail-closed.
- New operator paging/alerting when a revision fails ready.
- Exact Prod bootstrap values once Prod infra exists (must still match this policy bar; numeric probe/retry details may mirror QA).

## Considered options (rejected for now)

- **Separate migrate job before traffic** — cleaner isolation, but out of scope; we keep startup `MigrateAsync` with fail-closed ready + hard exit.
- **Deep schema diff in `/health/ready`** — stronger runtime check, deferred; CI + pending migrations + migrate success are the v1 bar.
- **Multiple revision mode / gradual traffic** — would not guarantee “last good keeps 100%” without extra routing policy; Single mode is required.

## Consequences

- Implementers must add pairing + `has-pending-model-changes` to CI, tighten ready + hard-exit in the API, and wire HTTP readiness (and Single mode) in QA/Prod Bicep — without reopening the policy above.
- Incomplete Designer commits and migrate failures become “stuck on old revision,” not “site-wide 500s,” once implementation lands.
- Reopen this ADR if we move migrations out of process, mandate deep schema validation at ready, or change revision-mode strategy.

## Implementation

Landed on the ADR branch (not a separate policy-only follow-up):

- `DatabaseInitState` + fail-closed `/health/ready` + hard `Environment.Exit(1)` after migrate retries — `backend/TummlyBackend`
- CI: `.github/workflows/backend-ci.yml` + gates in `qa-backend.yml`; scripts under `backend/TummlyBackend/scripts/` (pairing, list↔files, Restaurants→Users SET NULL ban)
- ACA probes + Single mode in `infra/qa/main.bicep` / `main.json`; probe patch step on QA deploy
- Agent diagnosis for *stuck revision*: `docs/agents/stuck-revision.md`, `scripts/probe-qa-api-revision.sh`

