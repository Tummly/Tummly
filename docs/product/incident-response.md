# Incident response

How to declare, contain, and close production incidents that affect personal data or Guest Loop availability.

## Status summary

| Area | Status |
|------|--------|
| IR runbook (this file) | Shipped |
| Automated paging / status page | Planned |
| On-call contact list | TBD |

## Purpose / scope

Use this runbook when production is affected by:

- **Security** — unauthorised access, credential leak, suspected breach
- **Availability** — Guest Loop, Operator, or Admin APIs down or severely degraded
- **Personal data** — guest or operator PII exposed, lost, or processed in error

Out of scope here: routine support tickets (see [support-playbooks.md](./support-playbooks.md)), planned maintenance, and non-production environments unless they hold live personal data.

---

## Severity

Align with support escalation placeholders.

| Severity | Meaning | Examples |
|----------|---------|----------|
| **P1** — cannot operate | Production stop or active personal-data risk | Mass email / SMS outage; Guest Loop unavailable for many venues; confirmed credential leak; suspected personal-data breach |
| **P2** — degraded | Limited impact; workaround exists | Single operator cannot sign in; one provider channel failing; elevated error rate on one API |
| **P3** — informational | No active harm; track and schedule | How-to questions; minor UI defect with no data risk; single failed job with automatic retry |

Declare the highest severity that fits. Escalate if impact grows.

---

## Roles

| Role | Responsibility | Contact |
|------|----------------|---------|
| **Incident Lead** | Declares severity, owns timeline, decides go/no-go for containment | TBD |
| **Comms** | Operator / stakeholder updates; public status if used | TBD |
| **Engineering** | Technical containment, fix, evidence collection | TBD |
| **Legal** (personal-data incidents) | ICO / regulatory assessment | TBD |

One person may hold more than one role on a small team. Record the named owners in the incident log at declare time.

---

## First 30 minutes checklist

1. **Detect** — Confirm the signal (alert, support report, audit anomaly). Note start time (UTC).
2. **Declare** — Assign Incident Lead. Set severity (P1 / P2 / P3). Open an incident channel or thread.
3. **Contain** — Stop ongoing harm where safe (revoke keys, disable a feature flag, rate-limit, take a failing path offline). Prefer reversible steps.
4. **Preserve evidence** — Do not wipe logs or rotate credentials before capturing what you need. Snapshot timestamps, request ids, and actor identities.
5. **Notify** — Tell Comms what operators or guests may see. For personal-data risk, notify Legal (TBD) within the first hour.

Do not wait for a full root cause before containment.

---

## Phases

### 1. Detect

- Source of signal (App Insights, support, provider dashboard, admin audit)
- Affected surface (Guest Loop / Operator / Admin / billing / messaging)
- First known impact time (UTC)

### 2. Contain

- Actions taken and by whom
- Blast radius (restaurants, guests, operators)
- Temporary workarounds for support

### 3. Eradicate

- Root cause (or best current hypothesis)
- Permanent fix or config change
- Verification that the cause cannot recur in the same way

### 4. Recover

- Restore full service
- Confirm data integrity where personal data was involved
- Clear residual alerts; return to normal monitoring

### 5. Post-mortem

Complete within a few working days after Recover for P1 and P2.

| Field | Content |
|-------|---------|
| Incident id / date | |
| Severity | P1 / P2 / P3 |
| Summary | 2–3 sentences |
| Timeline | Detect → Recover (UTC) |
| Impact | Who / what / how long |
| Root cause | |
| What went well | |
| What to improve | |
| Action items | Owner + due date |

---

## Evidence sources

| Source | Use for |
|--------|---------|
| Application Insights / app logs | Errors, latency, correlation ids |
| `GET /api/admin/audit-events` | Admin and system actions (including `retention.guest_purge`) |
| Revolut dashboard | Payment / merchant anomalies |
| Twilio dashboard | SMS delivery and inbound failures |
| Resend dashboard | Email delivery failures |
| Approved database path only | Confirmed data state — **no ad-hoc production writes**; use the documented / approved access path |

Do not pull production data into personal machines. Prefer read-only queries via the approved path.

---

## Personal-data incidents

If personal data may be breached, lost, or disclosed without authorisation:

1. Treat as **at least P2**; use **P1** when exposure is confirmed or ongoing.
2. Preserve evidence before rotating secrets or deleting records.
3. **ICO 72-hour assessment** — UK GDPR may require notification to the ICO within **72 hours** of becoming aware of a personal-data breach, when the breach is likely to result in a risk to individuals. Legal (TBD) owns the assess / notify decision.
4. Do not notify affected individuals unless Legal directs it.
5. Record assessment outcome and any notification id in the post-mortem.

Legal contact: **TBD**.

---

## Related documentation

| Need | Document |
|------|----------|
| Day-to-day support topics | [support-playbooks.md](./support-playbooks.md) |
| Roles and audit API | [security-and-rbac.md](./security-and-rbac.md) |
| Admin actions | [admin.md](./admin.md) |

## Not yet live

| Item | Status |
|------|--------|
| Named on-call / paging | TBD |
| Public status page | Planned |
| Automated containment feature flags | Planned |
