# AI Assistant question-first answers — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Operator AI Assistant answer the asked question first, with short supporting detail and at most one next action — not a keyword-triggered multi-domain dashboard dump.

**Architecture:** Keep Azure OpenAI, retrieve, create-draft, and safety refuses. Add `AssistantAskFocus` to classify the ask, filter evidence domains, and drive question-shaped copy in `AssistantLiveAnswerCopy` plus the live-answer structured-output prompt. Fix create-vs-mutate conflict and Change-scope dismiss. Do not replace the Assistant with a hosted chatbot.

**Tech Stack:** .NET Assistant helpers/services, xUnit (`TummlyBackend.Tests`), React Change-scope UI (`AiAssistantChangeScopeDialog`, `PerformanceDateRangeControl`).

**Tracker:** [.scratch/assistant-question-first/](../../../.scratch/assistant-question-first/map.md) — PRD + build tickets.

## Global Constraints

- Still refuse real send / mutate of live records from chat; Create Campaign Draft / Offer path / Recovery path stay allowed.
- Operator language only — no camelCase KPI names, no “Succeeded classification,” no “example eligibility,” no prompt jargon in user bodies.
- Answer shape: **direct answer → optional short detail → optional one action**.
- TDD: golden failing tests from tester examples before production copy changes.
- Ship in slices A–E; each slice must improve chat quality on its own.
- ASD-STE100 for agent reports to the human.

---

## Spec coverage (tester feedback)

| Feedback | Tasks |
|----------|-------|
| Direct, question-specific answers | 1–3 |
| No irrelevant zero-value data | 2–3, 5 |
| Do not return unrequested domains | 2–3 |
| QR / Capture understood | 1–2, 4 |
| Offer Redemptions correct domain | 1–2, 4 |
| Campaign create intent | 4 |
| Remove technical wording | 5 |
| Interpret, not print dashboard | 2–3, 5 |
| Answer hierarchy | 2–3, 5 |
| Scope dropdown dismiss | 6 |
| Natural question variants | 1, 4 |

---

## Task 1: Golden tests + `AssistantAskFocus`

**Files:**
- Create: `backend/TummlyBackend/Helpers/AssistantAskFocus.cs`
- Create: `backend/TummlyBackend.Tests/Helpers/AssistantAskFocusTests.cs`
- Create: `backend/TummlyBackend.Tests/Helpers/AssistantLiveAnswerCopyQuestionFirstTests.cs`
- Ticket: `.scratch/assistant-question-first/issues/01-golden-tests-and-ask-focus.md`

**Interfaces:**
- Produces:
  - `enum AssistantAskFocusKind { CampaignsActive, CampaignsAny, Feedback, OffersRedemptions, OffersClaims, CaptureQr, Performance, Guests, CreateCampaign, CreateOffer, MixedSummary, Unknown }`
  - `static AssistantAskFocusKind Detect(string userMessage)`
  - `static bool IncludesDomain(AssistantAskFocusKind focus, AssistantEvidenceDomain domain)` (or equivalent filter helper)

- [ ] **Step 1: Write failing focus + golden body tests**

Cover at least:

- `"Are there any active campaigns for this Location?"` → `CampaignsActive`
- `"Have we had any QR scans today?"` → `CaptureQr`
- `"Are there any Offer Redemptions today?"` → `OffersRedemptions`
- `"Can you create a Campaign?"` → `CreateCampaign`
- `"Any Campaigns live?"` / `"Did anyone redeem an Offer today?"` / `"Create a Campaign for recent guests."` → matching focuses
- Golden: active Campaigns ask with empty in-flight + one Draft + non-empty Feedback must **not** include Feedback / eligibility / Capture in body (will fail until Task 2–3)
- Golden: one Neutral Feedback only → no Positive/Negative zero lines (fail until Task 3)

- [ ] **Step 2: Run tests — expect fail**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~AssistantAskFocusTests|FullyQualifiedName~AssistantLiveAnswerCopyQuestionFirstTests"
```

- [ ] **Step 3: Implement `AssistantAskFocus.Detect` (minimal)**

Needle/synonym lists per focus; CreateCampaign beats Mutate; CaptureQr includes qr/scan variants; OffersRedemptions includes redeem/redemption.

- [ ] **Step 4: Re-run focus tests — pass; leave golden body tests failing**

- [ ] **Step 5: Commit** (only when human asks to commit)

---

## Task 2: Filter evidence by focus

**Files:**
- Modify: `backend/TummlyBackend/Helpers/AssistantLiveAnswerCopy.cs` (`GroundedFromEvidence` / evidence passed into body)
- Modify or create filter helper next to `AssistantAskFocus`
- Test: extend `AssistantLiveAnswerCopyQuestionFirstTests.cs`
- Ticket: `.scratch/assistant-question-first/issues/02-question-first-body-and-actions.md`

**Interfaces:**
- Consumes: `AssistantAskFocus.Detect`
- Produces: filtered `AssistantRetrievedEvidence` (empty unused domains) before body assembly and before Azure payload where the Fake/template path builds bodies

- [ ] **Step 1: Failing tests** — CampaignsActive keeps Campaigns facts only; OffersRedemptions drops Feedback; CaptureQr drops Feedback unless also asked

- [ ] **Step 2: Implement filter** — zero out or omit Feedback / Offers / Campaigns / Capture / Home / Guests domains not in focus

- [ ] **Step 3: Wire filter into `GroundedFromEvidence` (and Fake complete path if it calls that helper)**

- [ ] **Step 4: Run question-first tests**

- [ ] **Step 5: Commit** (when asked)

---

## Task 3: Rewrite `BodyFromEvidence` + one action

**Files:**
- Modify: `backend/TummlyBackend/Helpers/AssistantLiveAnswerCopy.cs` — `BodyFromEvidence`, `SummariseBody`, `CampaignsParts`, `CaptureParts`, `OffersParts`, `HomeParts`
- Modify: `backend/TummlyBackend/Helpers/AssistantActionCatalog.cs` — `DefaultActions` → at most one action for focus
- Ticket: `.scratch/assistant-question-first/issues/02-question-first-body-and-actions.md`

**Required behaviour:**

- Stop “if Feedback non-empty, always prepend Feedback.”
- CampaignsActive empty in-flight: start with  
  `No, there are no active or scheduled Campaigns for {location}.`  
  Optional one line if Drafts exist:  
  `You do have {n} Draft Campaign if you want to review it.`
- Feedback single Neutral:  
  `You received 1 piece of Feedback, classified as Neutral.`  
  Do not print Positive: 0 / Negative: 0.
- CaptureQr: QR count for period; do not auto-add Feedback submitted / opt-ins / previous window unless asked.
- OffersRedemptions: redemption counts from Offers Performance only.
- Hierarchy: direct → short support → stop.
- `DefaultActions`: ≤1 relevant action.

- [ ] **Step 1: Expand golden body assertions to exact/near-exact strings above**

- [ ] **Step 2: Implement body builders per focus**

- [ ] **Step 3: Cap actions**

- [ ] **Step 4: `dotnet test` filter on question-first + existing `AssistantLiveAnswerCopy` / conversation tests; fix regressions intentionally**

- [ ] **Step 5: Commit** (when asked)

**Slice A complete** when Tasks 1–3 pass.

---

## Task 4: Synonyms + create vs mutate

**Files:**
- Modify: `backend/TummlyBackend/Helpers/AssistantAskIntent.cs`
- Modify: `backend/TummlyBackend/Helpers/AssistantTaskClassification.cs`
- Modify: `backend/TummlyBackend/Helpers/AssistantAskFocus.cs` (align synonyms)
- Tests: `AssistantAskIntent` / `AssistantTaskClassification` / conversation create-path tests
- Ticket: `.scratch/assistant-question-first/issues/03-intent-synonyms-and-create-path.md`

**Required behaviour:**

- Expand in-scope / focus needles for live/sending/QR/redeem/feedback-this-week variants.
- `LooksLikeMutate` must **not** win over legal Create Campaign Draft (remove or gate `"create a campaign"` mutate needle when draft-create matches).
- Expand create needles: `create campaign` (no “a”), `can you create a campaign`, `start a campaign`, `help me create a campaign`, `create a campaign for recent guests`.
- Bare create starts Gap / goal ask — never `MutateRefusalBody` / vague allow-list clarify.

- [ ] **Step 1: Failing tests for create + synonym phrases**

- [ ] **Step 2: Implement intent + classification fixes**

- [ ] **Step 3: Run targeted tests**

- [ ] **Step 4: Commit** (when asked)

**Slice B complete.**

---

## Task 5: Structured-output prompt + operator language

**Files:**
- Modify: `backend/TummlyBackend/Helpers/AssistantLiveAnswerStructuredOutput.cs`
- Modify: remaining copy in `AssistantLiveAnswerCopy.cs` (HomeParts camelCase, stub sentences)
- Tests: `AssistantLiveAnswerStructuredOutputTests.cs` + question-first goldens
- Ticket: `.scratch/assistant-question-first/issues/04-prompt-and-operator-language.md`

**Required prompt rules (add/replace in system text):**

- Answer only what was asked; do not dump allow-list domains.
- Direct answer first; short detail; at most one next-step suggestion.
- Omit zero-value classification buckets.
- Never echo internal terms: current-state, eligibility keys, camelCase KPIs, “Succeeded classification.”
- Pass **focus** into the user payload so the model sees which domain to use.

- [ ] **Step 1: Failing prompt/assert tests for new instructions + HomeParts language**

- [ ] **Step 2: Update prompt + copy**

- [ ] **Step 3: Run tests**

- [ ] **Step 4: Commit** (when asked)

**Slice C complete.**

---

## Task 6: Change-scope dismiss

**Files:**
- Modify: `src/components/dashboard/operator/AiAssistantChangeScopeDialog.tsx`
- Modify: `src/components/dashboard/operator/Home/PerformanceDateRangeControl.tsx` (if period Popover is the stuck control)
- Tests: module/UI tests under `src/lib/operatorAiAssistant/` or component tests if present
- Ticket: `.scratch/assistant-question-first/issues/05-change-scope-dismiss.md`

**Required behaviour:**

- Outside click closes without Apply / without changing applied scope.
- Trigger again closes.
- Esc closes without changing applied scope.
- Nested Select/Popover above Dialog still paints correctly (`z-[130]`).

- [ ] **Step 1: Reproduce which control sticks (location Select vs period Popover)**

- [ ] **Step 2: Failing test or documented manual checklist + fix**

- [ ] **Step 3: Verify dismiss paths**

- [ ] **Step 4: Commit** (when asked)

**Slice D complete.**

---

## Task 7: Optional retrieve trim

**Files:**
- Modify: `backend/TummlyBackend/Services/AssistantConversationService.cs` — `RetrieveLocationDomainsAsync` / turn retrieve
- Ticket: `.scratch/assistant-question-first/issues/06-retrieve-trim-by-focus.md`

**Required behaviour:** Skip unused domain retrieves for narrow focus; keep full retrieve for `MixedSummary` / compare-all.

- [ ] **Step 1: Tests that narrow focus does not call unused retrieve fakes** (or integration equivalent)

- [ ] **Step 2: Wire focus into retrieve**

- [ ] **Step 3: Run Assistant conversation tests**

- [ ] **Step 4: Commit** (when asked)

**Slice E complete.**

---

## Out of scope

- Replacing Assistant with Chatbase / Dify / OpenAI Assistants-only product.
- Allowing send/schedule of Campaigns from chat.
- Rewriting all of `AssistantConversationService` in one PR.

## Human verification (after A–C)

Re-run tester script: active Campaigns, one Neutral Feedback, QR today, Offer redemptions, create Campaign, synonyms; confirm hierarchy and language; then scope dismiss (D).
