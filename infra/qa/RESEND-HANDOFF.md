# Resend handoff — verify `tummly.com` + QA / Prod email

Provider DNS: **Freeola / MyFreeola** (same zone as Azure QA hosts)  
Provider email: **[Resend](https://resend.com)**  
App config keys: `EmailSettings__*` (see `backend/TummlyBackend/.env.example`)

Related: Azure host DNS is in [`DNS-HANDOFF.md`](./DNS-HANDOFF.md). Add Resend records in the **same** Freeola custom DNS screen while you are there.

---

## Current state (today’s QA)

| Setting | Value | Why |
| --- | --- | --- |
| From | `Tummly <onboarding@resend.dev>` | Resend’s shared test sender until our domain is verified |
| Reply-To | `engineering@tummly.com` | Replies land in engineering |
| `EmailSettings__QaRedirectTo` | `engineering@tummly.com` | **Every** outbound mail is delivered to engineering; subject becomes `[QA for {original}] …`; OTP still validates against the address typed in the UI |

That setup is safe for QA, but From looks like Resend’s onboarding address and you cannot send as `@tummly.com` until the domain is verified.

---

## Recommended QA vs Prod differentiation

One verified domain (`tummly.com`) is enough for both environments. Differentiate with **app settings**, not a second DNS domain.

| | **Azure QA** | **Production** (later) |
| --- | --- | --- |
| Resend domain | `tummly.com` (shared, verified once) | same |
| API key | Shared key OK for now; split keys before public prod if you want blast-radius isolation | Dedicated prod key |
| `SenderEmail` | `qa@tummly.com` | `noreply@tummly.com` (or `hello@tummly.com`) |
| `SenderName` | `Tummly QA` | `Tummly` |
| `ReplyToEmail` | `engineering@tummly.com` | e.g. `support@tummly.com` when that inbox exists |
| `QaRedirectTo` | **Keep** `engineering@tummly.com` while overlapping / smoking Azure QA | **Unset** (empty) — real recipients |

### Why keep QA redirect after domain verify?

- Prevents accidental mail to real restaurant emails from QA data / trials.
- OTP / invite flows still work: engineering inbox gets the message; the app verifies the address the user entered.
- Drop `QaRedirectTo` only when you intentionally want “mail goes to the real `to:`” on QA (e.g. a controlled invite test).

### What changes after domain verify (Azure QA)

Update Container App / `secrets.qa.env` (then re-run `apply-aca-secrets.ps1`):

```text
EmailSettings__SenderName=Tummly QA
EmailSettings__SenderEmail=qa@tummly.com
EmailSettings__ReplyToEmail=engineering@tummly.com
EmailSettings__QaRedirectTo=engineering@tummly.com
```

Leave `EmailSettings__ApiKey` as today unless you create a new key.

Production (when you stand it up): same domain, different From, **no** `QaRedirectTo`.

---

## Part A — Resend dashboard (engineering)

1. Open [Resend → Domains](https://resend.com/domains).
2. **Add Domain** → `tummly.com`  
   - Region: pick closest useful region (e.g. EU if offered) and stick with it.
3. Open the new domain → **Records** / DNS table.
4. You should see roughly:
   - **DKIM** — `TXT` on something like `resend._domainkey`
   - **SPF** — `TXT` on something like `send`
   - **MX** — `MX` on something like `send` (feedback / bounce path; priority usually `10`)
   - Optional **DMARC** — only if Resend shows it and Freeola does not already have `_dmarc`
5. Use Resend’s **copy** buttons. Paste those exact values into the Freeola tables in Part B (do not invent values).
6. After Freeola save + propagation, click **Verify DNS Records** in Resend.
7. Wait until status is **Verified** (often minutes; can take up to ~72h).

Docs: [Add a domain](https://resend.com/docs/dashboard/domains/introduction) · [Troubleshooting](https://resend.com/docs/knowledge-base/what-if-my-domain-is-not-verifying)

**Email MX conflict:** Resend’s MX is on the **`send` subdomain**, not the apex. That should **not** replace Freeola’s normal `@` / apex MX for `engineering@tummly.com` inbox hosting. Do **not** change existing apex MX unless Resend explicitly asks for apex MX (it should not for sending-only verify).

---

## Part B — Freeola DNS (same flow as Azure QA)

1. [MyFreeola](https://freeola.com) → **DNS Records (A/MX/SPF/TXT)** → **DNS Settings**.
2. Tick **`tummly.com`** → **Change DNS** → **Create/Modify Custom DNS Records (Advanced)** → **Next**.
3. For each Resend row: pick type → **Add+** → fill **Subdomain** + value → **Save** when done.

Freeola appends `.tummly.com`. Enter only the host Resend shows **without** `tummly.com`  
(e.g. Resend name `send.tummly.com` → Subdomain `send`).

### Fill from your Resend Domains → Records tab

| # | Freeola type (drop-down) | Freeola **Subdomain** | Freeola value field | Paste from Resend |
| --- | --- | --- | --- | --- |
| 1 | **TXT** | *(e.g. `resend._domainkey`)* | **Value** | DKIM TXT content |
| 2 | **TXT** | *(e.g. `send`)* | **Value** | SPF TXT content (`v=spf1 …`) |
| 3 | **MX** | *(e.g. `send`)* | **Hostname or IP** (+ Priority if asked) | MX target + priority (often `10`) |
| 4 | **TXT** (optional) | `_dmarc` | **Value** | Only if Resend lists DMARC **and** you do not already have `_dmarc` |

If Freeola’s MX UI uses **Priority**, set it to whatever Resend shows (commonly `10`).

### Checklist (do not break email hosting)

- [ ] Apex / root **MX** for normal Freeola mail left unchanged  
- [ ] No extra quotes or spaces around TXT values  
- [ ] Subdomains do **not** include `.tummly.com`  
- [ ] Azure QA CNAMEs/TXTs from [`DNS-HANDOFF.md`](./DNS-HANDOFF.md) still present  

---

## Part C — After Resend shows Verified

1. Update Azure QA sender settings (table above) and apply secrets.
2. Send one OTP / trial mail from Azure QA.
3. Confirm engineering inbox gets `[QA for …]` mail **From** `qa@tummly.com` (not `onboarding@resend.dev`).
4. Smoke checklist item 6 (outbound email) can then use the branded From.

---

## Quick reference — env keys

```text
# Shared
EmailSettings__ApiKey=re_...

# Azure QA (after domain verified)
EmailSettings__SenderName=Tummly QA
EmailSettings__SenderEmail=qa@tummly.com
EmailSettings__ReplyToEmail=engineering@tummly.com
EmailSettings__QaRedirectTo=engineering@tummly.com

# Production (future)
EmailSettings__SenderName=Tummly
EmailSettings__SenderEmail=noreply@tummly.com
EmailSettings__ReplyToEmail=support@tummly.com
# EmailSettings__QaRedirectTo=   ← leave unset
```

---

## Optional later improvements (out of scope unless you ask)

- Separate Resend API keys for QA vs Prod  
- Verify a dedicated send subdomain (e.g. `mail.tummly.com`) for reputation isolation  
- Subject prefix for QA without redirect (would need a small code change; redirect already prefixes today)

Generated: 2026-07-17 — aligns with `EmailSettings.QaRedirectTo` behaviour in `EmailService`.
