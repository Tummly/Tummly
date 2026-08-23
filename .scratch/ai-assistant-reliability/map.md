# Wayfinder map: AI Assistant reliability (Offer path)

Label: `wayfinder:map`

Implement spec: [PRD.md](PRD.md) (`Status: ready-for-agent`).

## Destination

Root-cause fix of the existing **AI Assistant** create stack so multi-part Offer asks (create + Guest form thank-you attach), follow-up field fills, Gap ordinal choices, and replacement-item extraction produce correct **structured Offer path / Gap state**, validated tool arguments, and correct persistence — without a parallel orchestration, intent router, or post-hoc validation layer. Operator-facing Offer path persist copy and related Gap ask bodies improve only after state is correct; wording is secondary.

## Notes

- Domain: Operator **AI Assistant**; glossary in `CONTEXT.md` (**Offer path**, **Gap turn**, **Guest form thank-you attach**, **Offers catalog**). Update glossary only after code contracts change — do not treat `CONTEXT.md` as the fix.
- Skills: `/grilling`, `/domain-modeling`; implement against existing helpers/services, not a new agent stack.
- Wire task stays `AssistantTask.OfferPath`. Placement is optional state on Offer path terms / Gap, then completing turn: create catalog Draft → existing thank-you `SetAsync` → verify.
- Keep completing-turn persist; no new confirm button.
- Extend existing Gap option resolve for ordinals; no second choice engine.
- Reply style: `AssistantOfferPathPersistCopy` + related Gap ask bodies only (Campaign/Recovery only if same template leaks).
- Do not hardcode example strings from the reliability doc; tests use those phrases as fixtures only.
- Primary acceptance: structured state + tools + DB; presentation second.
- Codegraph: at most 2 `codegraph_explore` calls per turn.

## Decisions so far

- [**Offer path thank-you attach contract inventory**](issues/01-offer-path-thank-you-contract-inventory.md) — Thank-you `SetAsync` already accepts Draft; Offer path blocks on missing placement state, no `SetAsync` in persist, missing DI, and Draft-only / not-attached copy (research: `research/01-offer-path-thank-you-contract-inventory.md`).
- [**Separate offer type from replacement item text and ops**](issues/02-separate-type-from-item-and-ops.md) — Keep the two `ReplacementItemRegex` copies as the capture front; one shared helper truncates ops tails, strips one article, and discards filler. Set `replacement_item` with a null item when the name is missing or is ops/filler. Merge-fill a later bare item name. Do not start the retired draft interview. Leave free-item extract unchanged.
- [**Reject instruction-like offer field values before persist**](issues/07-reject-instruction-like-field-values.md) — Reject in `MissingFields` so the normal Gap opens. After `Apply`, reuse the ticket 02 helper on both item fields (clear or store the cleaned item). Do not add persist-only or catalog ops checks. Implementation waits for the spec after this map is complete.
- [**Gap option ordinal resolve**](issues/05-gap-option-ordinal-resolve.md) — Ordinal fallback on `AssistantCreateTargets.Resolve` and `AssistantCampaignDraftBind.ResolveNamedChoice` only; whole-message 1-based `Options` index; name/`Detect`/phrase first; no third choice engine, no location unique-name ordinals, no Recovery `number 1`/`option 1`, no Gap copy change. Implement in the spec after this map, not before.
- [**10 — Gap option ordinal resolve (spec)**](issues/10-gap-option-ordinal-resolve.md) — Implemented: shared `AssistantGapOptionOrdinal` fallback on the two resolvers only; SendTurn unnamed-create ordinals start Campaign; Recovery / location unchanged.
- [**Open Offer path Gap must not jump to Recovery**](issues/06-offer-path-gap-priority-over-recovery.md) — Hold inside `TryResumeGapAsync` only: when `AssistantTask = offer-path`, skip Detect-count > 1 replace and mismatch steal; fall through to `KindOfferTerms` / location / `ResumeOfferPathAsync`; never return `DraftTargets = [Recovery]`. Do not change `Detect` / `LooksLikeRecoveryAsk` or add a caller router. Explicit Recovery stays on the Offer Gap; cancel is the exit. Implement in the spec after this map, not before.
- [**Placement on Offer path terms and Gap state**](issues/03-placement-on-offer-path-state.md) — Optional `WantsAttach` + `Placement` (`guest_form_thank_you` only) on existing terms/Gap JSON. Silence = catalog only; generic attach = placement Gap; explicit Guest form thank-you = known. Strip attach clauses before benefits. Pick up in the spec after the map is done — do not implement from the grilling ticket.
- [**Completing Offer path creates draft then thank-you attach**](issues/04-persist-draft-then-thank-you-attach.md) — Keep attach inside `PersistCreateOfferDraftAsync` after `CreateDraftAsync`. Call existing `ICaptureThankYouOfferService.SetAsync(locationId, created.Id)` only when `Placement == guest_form_thank_you`. Verify from `SetResult.Ok` + matching id (no extra `GetAsync`). Inject the thank-you service on the Assistant ctor; `Program.cs` already registers it. Create-success + attach-fail keeps `CreatedOfferId`. Do not attach from Campaign-with-Offer persist. Copy sentences stay ticket 08.
- [**Offer path persist and Gap ask copy**](issues/08-offer-path-persist-and-gap-copy.md) — `AssistantOfferPathPersistCopy` reports ticket 04’s `ThankYouAttach` fact: catalog-only keeps Draft / not attached; verified attach uses title “Offers catalog Offer saved”, Status from `ThankYouOfferLive`, and “Attached to Guest form thank-you”; attach-fail keeps SuccessTitle (Draft saved) and must not use FailureTitle. `GapBody` never prints wire `placement`; dedicated Guest form thank-you ask. No Retrieve / Combined section rewrite. Implement in the spec after this map, not before.
- [**Tests for reliability failure modes**](issues/09-tests-reliability-failure-modes.md) — Extend `AssistantOfferPathTermsTests`, `AssistantCreateTargetsTests` / `AssistantCampaignDraftBindTests`, and `AssistantConversationServiceTests`. Lock create+attach parse, “14 days” stay-on-Offer, ordinals, missing replacement item, instruction-like reject, and no Recovery steal — via Gap JSON / tool args / DB, not reply prose. Rewrite `SendTurn_DraftInterview_TargetSwitch_ReplacesIncompleteState`. Pronoun “attach it” after a completed Offer stays fog. Implement in the spec after this map, not before.

## Not yet specified

- Whether “attach it” after a prior completed Offer (no open Gap) needs pronoun bind to `CreatedOfferId` in this effort, or only create+attach in one open Offer path.
- Closed phrase list for explicit Guest form thank-you language (fixtures in tests only). Campaign / Recovery Placement values stay out until the same bug is proven.

## Out of scope

- New `AssistantTask` value or parallel create+attach planner.
- New chat “Create draft” / confirm-before-persist UI.
- Rewrite of Retrieve / product-expert live answers.
- Hardcoded phrase patches for “capture thank you page”, “14 days”, “number 1”.
- Second intent router or AI validation middleware layered on unbroken extract.
