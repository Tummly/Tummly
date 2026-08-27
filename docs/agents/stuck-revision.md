# Stuck revision (QA / Prod API)

**Leading phrase:** *stuck revision* — GitHub Actions is green, but ingress still serves an older Container App revision because the new one never becomes Ready.

## When to use

A **new** API route returns empty **404** while a **known-old** authenticated route (for example `/api/auth/me` or `/api/restaurant/locations`) returns **401** without a token. That shape means the process answering traffic does not include the new controller, not that the SPA called the wrong path.

Also use when `/health/ready` on the *new* revision stays unhealthy after a backend push that changed migrations.

## Tight loop (agent-runnable)

From the repo root:

```bash
bash scripts/probe-qa-api-revision.sh
```

Optional overrides: `API_BASE`, `AZ_RESOURCE_GROUP`, `AZ_CONTAINER_APP`.

## What Ready means

See [ADR 0015](../adr/0015-deploy-schema-safety.md).

- **`latestRevisionName`** — newest revision created by deploy.
- **`latestReadyRevisionName`** — revision that may take traffic.
- When they differ, the new image is **stuck**: usually `MigrateAsync` failed (SQL error in Container App logs). Traffic stays on the last healthy revision. Green `qa-backend.yml` does **not** prove cutover.

## After the probe

1. Read Container App logs for the failing revision (`Error Number`, migration name).
2. Fix the migration or schema (common: SQL Server **1785** multiple cascade paths — see `CODING_STANDARDS.md` EF migrations).
3. Push and wait until **Ready** equals **Latest**, then re-run the probe.
