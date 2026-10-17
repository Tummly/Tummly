# Product demo (frontend)

Public E2E Guest Loop tour entry and session helpers.

| Path | Role |
|------|------|
| `/product-demo` | Bootstrap demo JWT, then open dashboard with entry overlay |
| `/scan/{token}?productDemo=1` | Guest Form sandbox (sessionStorage submit) |
| `/single-dashboard/*` | Real restaurant UI + entry / welcome / tour chrome |

Session keys: `tummly-product-demo-tour`, `tummly-product-demo-guest-submission`, `tummly-product-demo-guest-scan-path`.

APIs: `GET /api/auth/demo-guest-entry`, `POST /api/auth/demo-session`.

§14 checklist: entry paths, guest sandbox isolation, welcome + 4 chapters / 17 steps, coachmark navigation, neutral vs “your feedback” copy, conversion modal, side effects remain suppressed by backend demo JWT.