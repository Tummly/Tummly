# Activation gate is a pure decision module, separate from state classification and code crypto

The **Activation gate** — the access rule that blocks the **Operator dashboard** / operator APIs until **Account activation** succeeds, and blocks **Sign-in** entirely once **Activation expired** — is implemented by a single pure module `ActivationGate` whose only method is `Decide(subject, intent) -> ActivationDecision`. It takes a narrow `ActivationSubject` record (`ActivatedAt`, `ActivationExpiresAt`, `ActivationCodeHash`) and an `ActivationIntent` (`SignIn` | `ApiAccess`), and returns a result object. It never throws, never touches the database, and never builds HTTP responses.

Two thin adapters translate decisions into side effects: `ActivationGateMiddleware` calls `Decide(subject, ApiAccess)` per request and maps a `Block` decision's `Reason` (`Pending` | `Expired`) into the existing 403 JSON shapes (`activationRequired` / `activationExpired`); `AuthService` calls `Decide(subject, SignIn)` at the OTP-verify and credential-validate sites and throws `ActivationExpiredException` on a `Block`.

We split activation into **three** small deep modules rather than one fat one:

- `ActivationGate` — the access-rule decision (one method, pure).
- `ActivationState` — state classification (`RequiresActivation`, `IsActivationExpired`, `IsWithinActivationPeriod`, `HasActivationState`, `GetStatusDetail`) consumed by the gate, the `activationRequired` sign-in routing field, and admin status display. Pure functions over `ActivationSubject`.
- `ActivationCodeHelper` — the **Activation Code** crypto only (generate, normalize, validate format, hash, verify, format for display) plus activation-period timestamp math. The gate predicates and display helpers that previously lived here were removed.

We rejected a fat `ActivationGate` that also owns state classification and exposes the predicates on its interface. The glossary defines the **Activation gate** as "the access rule" — a verdict, not a classifier. Routing (`activationRequired = RequiresActivation(user)`) and admin badge display are classification consumers, not gate consumers; folding them in would broaden the gate's interface past its glossary definition and recreate the "two ways to ask the same question" leak the refactor removes. A narrow gate also makes the 4-states × 2-intents decision table trivially unit-testable with no DB or HTTP harness.

We rejected a generic repository / DbContext-inside-the-gate design: the gate takes a record, so the data fetch (middleware's `AsNoTracking()` lookup, AuthService's already-loaded `User`) stays in the adapter where it belongs. The decision is now testable independently of how the user row was loaded.

We rejected throwing from `Decide` itself. The two adapters react differently (HTTP 403 JSON vs. sign-in exception), so a throw would force the middleware to catch-and-translate. Returning a result keeps the gate a pure function and puts side effects in adapters.

The frontend wire contract (`activationRequired` / `activationExpired` 403 flags consumed by the axios interceptor) is preserved exactly — the message strings move from `ActivationCodeHelper` into the gate, but the JSON shapes are still built by the middleware. No frontend change is required for this refactor.

Reopen this ADR if a third intent appears (e.g. a background job that needs to decide whether to sweep expired accounts), if the gate needs to consult data beyond the three `ActivationSubject` fields, or if a second activation-related flow makes merging the three modules back together the cheaper move.
