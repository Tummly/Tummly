# Security, authentication, and RBAC

Roles, access control, session handling, and tenant isolation for Tummly.

## Status summary

| Feature | Status |
|---------|--------|
| JWT authentication | Shipped |
| Admin vs operator separation | Shipped |
| Activation gate | Shipped |
| Owned location isolation | Shipped |
| Invite token security | Shipped |
| Password hashing (BCrypt) | Shipped |
| Sign-in OTP | Shipped |
| Trusted device | Shipped |
| Failed login / account lock fields | Shipped — **5 attempts** → `IsLocked` (operators on `universal-login`; admins on `admin-login` only) |
| Guest feedback rate limit | Shipped |
| Address lookup rate limit | Shipped |
| Audit logging | Shipped (append-only table + `GET /api/admin/audit-events`; no Admin UI) |
| Fine-grained permissions | Planned |
| MFA beyond OTP | Planned |

## Domain terms

| Term | Definition |
|------|------------|
| **Activation gate** | Middleware blocking operator APIs until **Account activation** or when **Activation expired** |
| **Owned location** | Location whose restaurant is owned by the signed-in operator |
| **Trusted device** | 30-day browser trust after Sign-in OTP |

---

## Roles

| | |
|---|---|
| **Status** | Shipped |
| **Launch blocker** | None |

### Permissions matrix

| Actor | Role claim | Access | Status |
|-------|------------|--------|--------|
| Platform admin | `Admin` (JWT claim) | `/api/admin/*`, admin dashboard | Shipped |
| Operator | `Owner` on `Users` (JWT claim); client session role `USER` | Operator APIs for **Owned location**s when activated | Shipped |
| Guest | None | `/api/scan/{token}/*` only | Shipped |
| Anonymous | None | Self-service Pilot (Planned), address lookup, Team invitation `/start`, demo/sales invite validate + setup-account, legacy Trial Request | Shipped (Trial + setup); Self-service Planned |

**No sub-roles** for operators today (no staff/manager RBAC).

### Entry points

- JWT: `JwtService.cs` — embeds role (`Admin` or `Owner`), user id
- Admin guard: `[Authorize(Roles = "Admin")]` on `AdminController`
- Frontend: `RoleRoute role="ADMIN"` for `/admin-dashboard` (session role `ADMIN`, mapped from admin JWT at login)

---

## Tenant isolation

| | |
|---|---|
| **Status** | Shipped |
| **Launch blocker** | None |

### Rules

- Operators access only locations where `Restaurant.OwnerUserId == authenticated User.Id`
- Enforced in `OwnedLocationService` + `OwnedLocationResponses` for feedback and location-scoped operator APIs
- `RestaurantController.GetLocations` scopes to owner's restaurant
- Guest access scoped by opaque `QrCode.Token` — no cross-location enumeration

### Edge cases

| Case | Response |
|------|----------|
| Operator requests another operator's `locationId` | 403 Forbidden |
| Guest uses wrong token | 404 Not found |
| Admin accesses operator data | Via admin APIs (trial/operator), not location impersonation |

---

## Session handling

| | |
|---|---|
| **Status** | Shipped |
| **Launch blocker** | None |

### Client

| Mechanism | Storage | Purpose |
|-----------|---------|---------|
| JWT | `authStore` (localStorage `tummly-auth`) | API Authorization header |
| Trusted device token | localStorage | Skip OTP on `universal-login` |
| Cookie consent | `cookieConsentStore` | Analytics only |

Full cookie/storage inventory: [cookie-storage-inventory.md](./cookie-storage-inventory.md).

### Server

| Mechanism | Purpose |
|-----------|---------|
| JWT expiry | Configured in `JwtSettings` |
| `RefreshTokens` table | Refresh token support in schema |
| Sign out | Clears JWT client-side; keeps device token |

### Axios interceptors

| Status | Action |
|--------|--------|
| 401 | Clear session → `/login` |
| 403 `activationRequired` | Redirect `/login?step=activation-code` |
| 403 `activationExpired` | Clear session → `/login` |

`skipAuthRedirect` config option for `/auth/me` during routing.

---

## Sign-in security

| | |
|---|---|
| **Status** | Shipped |
| **Launch blocker** | None |

### Controls

- Password stored as BCrypt hash
- **Sign-in OTP** — 10-minute expiry (email template copy)
- **First Sign-in** always requires OTP
- **New device sign-in** notification email on OTP success
- **Activation expired** blocks login before JWT issued

### Partial

- `FailedLoginAttempts`, `IsLocked` on `User` and `Admin` — **5 failed password attempts** locks account. Operator lock enforced on `universal-login` operator path. **Admin lock not enforced on `universal-login`** (only on `admin-login`).

---

## Operator Setup link security

Demo/sales **Operator Setup invitation** only. Public Self-service Pilot does not use this token. **Team invitation** uses a separate **opaque invitation reference** on `/start?invite=` (7-day expiry; rotate on Resend) — see [team.md](./team.md).

| | |
|---|---|
| **Status** | Shipped |
| **Launch blocker** | None |

### Controls

| Control | Detail |
|---------|--------|
| Invite token | GUID; rotated on resend/reminder |
| Expiry | 14 days from send |
| One-time account | `IsAccountCreated` / conflict if repeat setup |
| HTTPS | Expected in production `Frontend:BaseUrl` |

Endpoints: `validate-invite`, `setup-account`, `generate-activation-code` — unauthenticated but token-gated.

---

## Password reset

| | |
|---|---|
| **Status** | Shipped |

- Reset token via `PasswordResets` table
- Email link to `/reset-password?token=`
- Password changed notification email on success
- Minimum strength **Good** on client

---

## Rate limiting and abuse

| Surface | Limit | Status |
|---------|-------|--------|
| Guest feedback per token | 10 / hour | Shipped (memory cache) |
| Guest Form thank-you new issues per token | 5 / hour | Shipped (memory cache; re-shows exempt) |
| Guest Form thank-you new issues per IP | 10 / hour | Shipped (memory cache; re-shows exempt) |
| Guest Form reCAPTCHA v3 | Required when `Recaptcha:SecretKey` set | Shipped |
| Activation code verify per user | 5 attempts / 15 min | Shipped (memory cache) |
| Address suggest / resolve | 60 / 30 per 5 min (defaults) | Shipped |
| Trial OTP resend (demo/sales) | 60s cooldown; max 5 resends | Shipped |
| Self-service Email verification resend | TBD at build | Planned |
| AspNetCoreRateLimit package | Not registered in `Program.cs` | Not active |

---

## No-permission states

| Scenario | User experience | HTTP |
|----------|-----------------|------|
| No JWT on protected route | Redirect `/login` | 401 API |
| Wrong role (operator → admin route) | Redirect `/login` via `RoleRoute` | 403 admin API |
| Pending activation | Activation screen; APIs blocked | 403 `activationRequired` |
| Activation expired | Sign-in error / session cleared | 403 `activationExpired` |
| Non-owned location | API error | 403 |
| Invalid guest token | Guest not-found page | 404 |

---

## Audit logging

| | |
|---|---|
| **Status** | Shipped (append-only table + `GET /api/admin/audit-events`; no Admin UI) |
| **Launch blocker** | Cleared — admin/support audit, guest retention purge, and [incident-response runbook](./incident-response.md) shipped |

### Shipped

| Data | Location |
|------|----------|
| Admin/support audit events | `AdminAuditEvents` — list via `GET /api/admin/audit-events` |
| Trial review metadata | `TrialRequests.ReviewedBy`, `ReviewedAt`, status messages |
| Invite timestamps | `InviteSentAt`, `InviteExpiresAt` |

### Planned

- Sign-in history beyond "new device" email
- Guest data export/deletion audit

---

## Compliance dependencies

| Area | Dependency | Status |
|------|------------|--------|
| Marketing analytics | Cookie consent + Cookie Policy | Shipped |
| Guest personal data | Privacy Policy; feedback stored per location | Shipped |
| Trial PII | Email, phone, address in `TrialRequests` | Shipped |
| Password storage | BCrypt | Shipped |
| UK address lookup | Ideal Postcodes processor | Shipped |
| Public review manipulation | FAQ policy — do not gate reviews | Copy only |

---

## Flow diagram

```mermaid
flowchart TD
    REQ[API request] --> AUTH{JWT valid?}
    AUTH -->|No| U401[401]
    AUTH -->|Yes| ROLE{Admin?}
    ROLE -->|Yes| ADMIN[Admin controllers]
    ROLE -->|No| GATE{Activation gate}
    GATE -->|Blocked| U403[403 activation]
    GATE -->|OK| OWN{Owned location?}
    OWN -->|No| U403b[403]
    OWN -->|Yes| OK[200]
```

## Not yet live

| Item | Status |
|------|--------|
| Admin audit log | Shipped (append-only table + `GET /api/admin/audit-events`; no Admin UI) |
| Operator staff roles | Planned |
| IP-based blocking | Planned |
| WAF / DDoS (platform-level) | Operational — Railway/Vercel |
| Secrets rotation runbook | Operational (manual) |

## Implementation notes

- `ActivationGateMiddleware.cs` — allowed path prefixes
- `OperatorAuth.cs`, `OwnedLocationService.cs`
