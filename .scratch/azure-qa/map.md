# Azure QA on tummly.com

Label: `wayfinder:map`

## Destination

A live Azure QA environment — frontend at `qa.tummly.com`, API at `api.qa.tummly.com`, with Azure SQL and private Blob — proven with a short overlap against today’s Vercel / Railway / DigitalOcean stack, then that old stack retired. No production stand-up in this effort.

## Notes

- **Domain:** infra / platform; not product glossary work unless hostnames leak into operator-facing copy.
- **Execution is in scope.** This map carries standing up QA, not only deciding how. Prefer decisions first; tasks that provision, wire, or cut over belong here when sharp.
- **Skills:** `/grilling`, `/domain-modeling`, `/research` as ticket types require. Prefer Lucide/shadcn only if UI appears (unlikely).
- **Standing preferences (from charting):**
  - Prod-shaped, sized down: Azure Static Web Apps, Container Apps, Azure SQL, Blob; no Redis for single-instance QA (ADR-0011).
  - Region: UK South.
  - Fresh SQL + Blob (no DO data migrate).
  - Deploy: GitHub Actions from the `qa` branch.
  - IaC: hybrid Bicep for the core resource set.
  - Secrets: GitHub Actions secrets + Azure app settings for QA; Key Vault later (before prod).
  - Object storage: Azure Blob SDK path (not Spaces gateway).
  - DNS: external owner publishes records; we supply a handoff doc.
  - Azure subscription already exists.
- **Current hybrid QA (to retire after overlap):** Vercel FE, Railway API, DO SQL Server VPS, DO Spaces — see `backend/DEPLOYMENT.md`.

## Decisions so far

- [DNS records for SWA and Container Apps custom domains](issues/01-dns-records-for-swa-and-container-apps.md) — SWA: CNAME `qa` + TXT `_dnsauth.qa`; ACA: CNAME `api.qa` + TXT `asuid.api.qa`; no proxy on ACA CNAME; DigiCert CAA if CAA exists.
- [Minimal Azure SKUs for QA in UK South](issues/04-minimal-qa-skus.md) — Free SQL (or S0), ACA Consumption 0.5/1Gi one replica, SWA Free, ACR Basic, Blob Hot LRS; ~$15–55/mo light QA
- [GitHub Actions auth to deploy Azure QA](issues/05-github-actions-azure-auth.md) — Entra OIDC for ACA/ACR; SWA deployment token for frontend
- [Resource group and naming for Azure QA](issues/02-resource-group-and-naming.md) — One RG `rg-tummly-qa`; CAF names (`swa-tummly-qa`, `ca-tummly-qa-api`, `sttummlyqa`, …); region in location only
- [Blob provider strategy during QA overlap](issues/03-blob-provider-during-overlap.md) — `Provider` switch: Azure QA → Blob; local + Railway overlap → Spaces; Azurite later
- [Smoke checklist before retiring old QA](issues/06-retire-smoke-checklist.md) — Health/ready, sign-in, FE→API, attachment, email, GHA deploy; day-to-day QA owner signs off
- [Reuse vs duplicate Azure OpenAI and Speech for QA](issues/07-reuse-azure-ai-resources.md) — Reuse existing OpenAI + Speech keys/endpoints in QA app settings; no QA-only AI resources
- [Author hybrid Bicep and provision QA core](issues/08-author-bicep-and-provision.md) — `rg-tummly-qa` live; SWA eastus2, SQL centralus (sub limits), rest uksouth; placeholder API until GHA
- [Implement Azure Blob object storage path](issues/09-implement-azure-blob-storage.md) — `Provider` S3|AzureBlob; Blob via MI or connection string; fail-fast unchanged
- [GitHub Actions deploy from qa branch](issues/10-github-actions-qa-deploy.md) — `qa-frontend` + `qa-backend` workflows; OIDC + SWA token; repo secrets set; merge onto `qa` to run
- [Wire secrets and verify Azure QA](issues/12-wire-secrets-and-verify-qa.md) — ACA env from `secrets.qa.env`; SQL synced; `/health` + `/health/ready` 200 on default hosts; custom-domain re-check after 11

## Not yet specified

- Observability depth (App Insights alerts, log retention) beyond “health endpoints work.”
- How many calendar days of overlap before retire (checklist gates readiness; calendar is soft).
- Local Azurite / Blob-emulator parity (deferred after Spaces retire).
- Whether to request UK South SQL quota so SQL can move closer to ACA later.
- Cleanup of failed `sql-tummly-qa` (uksouth) / `sql-tummly-qa-eastus2` leftovers if still present.

## Out of scope

- Production environment on Azure / `tummly.com` apex cutover.
- Migrating the whole DNS zone to Azure DNS.
- Azure Cache for Redis on QA (until multi-instance).
- Key Vault as the QA secrets store (deferred until before prod).
- Migrating existing DO SQL / Spaces data into Azure.
