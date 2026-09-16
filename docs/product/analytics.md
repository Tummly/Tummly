# Analytics and event map

How usage is measured today and the **Target** instrumentation spec for funnels.

## Status summary

| Feature | Status |
|---------|--------|
| Google Analytics 4 (gtag) | Shipped (requires `VITE_GA_MEASUREMENT_ID`; no-op when unset) |
| Cookie consent gating | Shipped |
| Page views | Shipped |
| Custom events | Partial — Campaigns MVP minimal send set via server log sink (ticket 32); GA custom events still Planned |
| Funnel dashboards | Planned |
| Server-side analytics | Partial — `ICampaignProductAnalytics` log sink; product analytics DB still Planned |

## Domain terms

| Term | Definition |
|------|------------|
| **Consent** | User choice in cookie banner — analytics runs only when accepted |
| **page_view** | GA config hit on route change with `page_path` |

---

## Analytics stack

| | |
|---|---|
| **Status** | Shipped |
| **Compliance** | Cookie Policy; reject non-essential blocks GA |
| **Launch blocker** | None if consent flow kept |

### Components

| Piece | Location |
|-------|----------|
| GA measurement ID | `VITE_GA_MEASUREMENT_ID` env — analytics disabled when empty |
| Consent store | `cookieConsentStore.ts` |
| Init + page views | `analytics.ts`, `GoogleAnalytics.tsx` |
| Banner | `CookieConsentBanner.tsx` |

### Behaviour

1. User accepts cookies → `setAnalyticsConsent(true)` → load gtag script.
2. On route change (`pathname`, `search`, `hash`) → `trackPageView` if consent granted.
3. Reject → `ga-disable-{id}` flag; no script load.

---

## Shipped events

| Event | Properties | Fires where | Status |
|-------|------------|-------------|--------|
| `page_view` (gtag config) | `page_path` | All routes after consent | Shipped |
| `cookie_consent_granted` | — | Not implemented as named event | — |
| `cookie_consent_rejected` | — | Not implemented as named event | — |

### Campaigns MVP (server adapter)

Minimal send set via `ICampaignProductAnalytics` (ticket 32). Production uses `LoggingCampaignProductAnalytics` until a GA / product sink is wired. Full draft §26 catalogue stays deferred.

| Event | Properties | Fires where | Status |
|-------|------------|-------------|--------|
| `campaign_schedule_commit` | `campaignId`, `mode` (`send-now` \| `schedule-later`) | Successful schedule / send-now commit | Shipped (log sink) |
| `campaign_send_start` | `campaignId` | Fire path passed cannot-start gates | Shipped (log sink) |
| `campaign_send_terminal` | `campaignId`, `status` (`sent` \| `partially-sent` \| `failed`) | Terminal fire outcome | Shipped (log sink) |
| `campaign_send_test` | `locationId` | Successful Campaign send test (no credit burn) | Shipped (log sink) |

**Implicit:** First `page_view` after accept fires for current route only.

### Routes generating page_view (when consented)

| Path pattern | Context |
|--------------|---------|
| `/` | Marketing homepage |
| `/privacy`, `/terms`, `/cookie-policy` | Legal |
| `/login`, `/forgot-password`, `/reset-password` | Auth |
| `/start` | Team invitation accept |
| `/setup-account*` | Demo/sales Operator Setup |
| `/single-dashboard`, `/multi-dashboard`, `/admin-dashboard` | Dashboards |
| `/scan/:token` | Guest feedback |

---

## Target events (Planned)

Priority for implementation. Not fired in codebase today.

### Acquisition funnel

Self-service Pilot is the public path. Legacy Trial Request event names remain for demo/sales until that path is retired.

| Event | Properties | Funnel step | Intended fire location | Priority |
|-------|------------|-------------|------------------------|----------|
| `self_service_started` | `entry` (home\|pricing\|social) | Acquisition | Get started / Start 30-day Pilot / social | P1 |
| `email_verification_sent` | — | Acquisition | Verification link sent | P1 |
| `email_verification_completed` | — | Acquisition | Link confirmed (or social skip) | P1 |
| `trial_request_started` | — | Acquisition (demo/sales) | Legacy `HeroTrialForm` submit | P3 |
| `trial_otp_sent` | — | Acquisition (demo/sales) | After `request-trial` success | P3 |
| `trial_otp_verified` | — | Acquisition (demo/sales) | `HeroTrialOtpStep` success | P3 |
| `trial_request_success_view` | — | Acquisition (demo/sales) | `HeroTrialSuccessStep` mount | P3 |

### Onboarding funnel

| Event | Properties | Funnel step | Intended fire location | Priority |
|-------|------------|-------------|------------------------|----------|
| `guest_loop_onboarding_step_completed` | `step`, `account_type` | Onboarding | Each Guest Loop onboarding step | P1 |
| `plan_choice_selected` | `plan` | Onboarding | Plan choice continue | P1 |
| `setup_invite_opened` | `account_type` | Onboarding (demo/sales) | `validate-invite` success | P2 |
| `operator_setup_step_completed` | `step`, `account_type` | Onboarding (demo/sales) | Each Operator Setup wizard step | P2 |
| `operator_setup_completed` | `account_type`, `location_count` | Onboarding | Provisioning Ready success (both paths) | P1 |
| `activation_code_generated` | — | Onboarding | Phase 3 complete | P2 |
| `team_invite_accept_started` | — | Onboarding | `/start?invite=` valid | P2 |
| `team_invite_accepted` | — | Onboarding | Membership created | P2 |

### Activation funnel

| Event | Properties | Funnel step | Intended fire location | Priority |
|-------|------------|-------------|------------------------|----------|
| `sign_in_started` | — | Activation | `SignInForm` submit | P1 |
| `sign_in_otp_verified` | `channel` | Activation | OTP success | P1 |
| `account_activated` | — | Activation | `SignInActivationCodeStep` success | P1 |
| `activation_expired_shown` | — | Activation | Sign-in error for expired | P2 |

### Operator product

| Event | Properties | Funnel step | Intended fire location | Priority |
|-------|------------|-------------|------------------------|----------|
| `smart_guest_link_copied` | `location_id` | Engagement | Home copy Smart Guest Link | P2 |
| `dashboard_location_switched` | `location_id` | Engagement | Multi dashboard switcher | P3 |

### Guest

| Event | Properties | Funnel step | Intended fire location | Priority |
|-------|------------|-------------|------------------------|----------|
| `guest_scan_loaded` | — | Guest | `GuestFeedbackPage` metadata OK | P1 |
| `guest_feedback_submitted` | `contact_type` | Guest | Form success | P1 |
| `guest_feedback_rate_limited` | — | Guest | 429 handler | P3 |

### Admin

| Event | Properties | Funnel step | Intended fire location | Priority |
|-------|------------|-------------|------------------------|----------|
| `trial_approved` | `trial_request_id` | Admin | Approve confirm | P2 |
| `trial_declined` | — | Admin | Decline confirm | P2 |
| `activation_extended` | `user_id` | Admin | Extend activation | P3 |

---

## Funnels (Target)

```mermaid
flowchart LR
    subgraph Acquisition
        A1[self_service_started] --> A2[email_verification_completed]
    end
    subgraph Onboarding
        B1[guest_loop_onboarding_step_completed] --> B2[plan_choice_selected]
        B2 --> B3[operator_setup_completed]
    end
    subgraph Activation
        C1[sign_in_otp_verified] --> C2[account_activated]
    end
    subgraph Guest
        D1[guest_scan_loaded] --> D2[guest_feedback_submitted]
    end
    A2 --> B1
    B3 --> C1
```

**Shipped measurement today:** only route-level `page_view` — funnels must be approximated manually in GA4 until Target events ship.

---

## Cross-reference rule

Other `docs/product/*.md` screen tables use:

- Shipped: `page_view`
- Planned: event name with `(Planned)` suffix

---

## Not yet live

| Item | Status |
|------|--------|
| Custom gtag events | Planned |
| GA4 funnel explorations (configured) | Planned |
| Server-side / product analytics DB | Planned |
| Consent mode v2 advanced | Planned |
| Error tracking (Sentry etc.) | Planned |

## Implementation notes

- To add events: extend `analytics.ts` with `trackEvent(name, params)` called behind consent check
- Do not fire PII in event properties (email, phone, guest name)
