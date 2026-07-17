# DNS handoff — Azure QA on Freeola (`tummly.com`)

Provider: **Freeola / MyFreeola**  
Goal: add **4 DNS records** so Azure can serve:

- Frontend → `https://qa.tummly.com`
- API → `https://api.qa.tummly.com`

When finished, message engineering — we bind certificates and update app URLs.

Also while in Freeola DNS: verify **Resend** for `@tummly.com` sending — see [`RESEND-HANDOFF.md`](./RESEND-HANDOFF.md) (extra TXT/MX on the `send` / DKIM hosts; does not replace Azure rows below).

Official Freeola guide (for reference): [Setting Custom DNS Records](https://freeola.com/support/domain-dns-setting.html)

---

## 1. Open the DNS editor

1. Log in to [MyFreeola](https://freeola.com).
2. Left menu: **DNS Records (A/MX/SPF/TXT)** → **DNS Settings**.
3. Tick **`tummly.com`**.
4. Click **Change DNS**.
5. Choose **Create/Modify Custom DNS Records (Advanced)** → **Next**.
6. Accept the warning page if shown.

You should now see the page where you pick a record type from the drop-down and click **Add+**.

---

## 2. Add the four records

Use **Add+** once per row. Freeola appends `.tummly.com` for you — type only what is in **Subdomain** (do **not** type `tummly.com`).

Leave **Priority** blank where it appears.

### Record A — TXT (Azure Static Web Apps ownership)

1. Drop-down: **TXT - Associate additional information (TXT)** → **Add+**
2. Fill in:

| Freeola field | Paste this |
| --- | --- |
| **Subdomain** | `_dnsauth.qa` |
| **Value** | `_5603085548b8qcl4ollrk7ildmiteaw` |

### Record B — CNAME (frontend traffic)

1. Drop-down: **CNAME - Point Web Address to a hostname** → **Add+**
2. Fill in:

| Freeola field | Paste this |
| --- | --- |
| **Subdomain** | `qa` |
| **Target Hostname** | `polite-meadow-07399960f.7.azurestaticapps.net` |

### Record C — TXT (Azure Container Apps ownership)

1. Drop-down: **TXT - Associate additional information (TXT)** → **Add+**
2. Fill in:

| Freeola field | Paste this |
| --- | --- |
| **Subdomain** | `asuid.api.qa` |
| **Value** | `DC5D0E3F7CAF9AB693B6A092401AA4BB53BBEC0FE5DC8CEC4DBCB82A59CF5B5C` |

### Record D — CNAME (API traffic)

1. Drop-down: **CNAME - Point Web Address to a hostname** → **Add+**
2. Fill in:

| Freeola field | Paste this |
| --- | --- |
| **Subdomain** | `api.qa` |
| **Target Hostname** | `ca-tummly-qa-api.agreeablewater-62e50314.uksouth.azurecontainerapps.io` |

### Save

Click **Save** once all four rows look correct.

---

## 3. Checklist before you leave Freeola

You should see something equivalent to:

| Type | Subdomain | Points to / value |
| --- | --- | --- |
| TXT | `_dnsauth.qa` | `_5603085548b8qcl4ollrk7ildmiteaw` |
| CNAME | `qa` | `polite-meadow-07399960f.7.azurestaticapps.net` |
| TXT | `asuid.api.qa` | `DC5D0E3F7CAF9AB693B6A092401AA4BB53BBEC0FE5DC8CEC4DBCB82A59CF5B5C` |
| CNAME | `api.qa` | `ca-tummly-qa-api.agreeablewater-62e50314.uksouth.azurecontainerapps.io` |

**Do not change** existing MX / email records.  
**Do not** use Freeola “Create/Modify subdomains to use with Web Hosting” for this — use **Custom DNS Records (Advanced)** as above.

---

## 4. Optional: CAA (only if you already have CAA)

If `tummly.com` already has CAA records, also allow DigiCert:

| Type | Subdomain | Value |
| --- | --- | --- |
| CAA | *(blank / `@`)* | `0 issue "digicert.com"` |

If you have **no** CAA records today, skip this.

---

## 5. How long / how to check

Freeola: usually **under a couple of hours**, can take up to **24 hours**.

From a terminal (optional):

```text
nslookup -type=TXT _dnsauth.qa.tummly.com
nslookup -type=CNAME qa.tummly.com
nslookup -type=TXT asuid.api.qa.tummly.com
nslookup -type=CNAME api.qa.tummly.com
```

When those resolve, tell engineering. We will:

1. Finish Azure binding for `qa.tummly.com` and `api.qa.tummly.com` (+ SSL)
2. Point CORS / frontend base URL / `VITE_API_BASE_URL` at the new hosts

---

Generated: 2026-07-17 — tokens from live `rg-tummly-qa`.
