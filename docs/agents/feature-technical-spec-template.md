# Functional & Technical Feature Specification

**Feature name:** [Short name]  
**Status:** Draft | In review | Approved for build  
**Owner (product):** [Name]  
**Owner (engineering):** [Name]  
**Last updated:** [YYYY-MM-DD]  
**Related designs:** [Full Figma URLs with node-id]  
**Related product notes (optional):** [Link]

> Use this template when a non-technical author (often with AI help) must give an engineer enough clarity to build a feature from the ground up.  
> Prefer tables over long paragraphs. Use one meaning per term. Mark unknowns as Open questions — do not invent APIs or storage shapes.

---

## 1. Purpose

### 1.1 Goal
Write one short paragraph. State what the user can do after this feature exists.

### 1.2 Why it matters
State the business or user problem in plain words. Do not describe implementation.

### 1.3 Definition of done
List clear checks that prove the feature is complete. Each check must be testable.

- [ ] ...
- [ ] ...

---

## 2. Scope

### 2.1 In scope
List every part that this work includes.

- ...

### 2.2 Out of scope
List related ideas that this work does **not** include.

- ...

### 2.3 Assumptions
List facts the team treats as true while they build.

- ...

### 2.4 Constraints
List hard limits (time, tools, legal rules, brand rules, platform limits).

- ...

---

## 3. Users and access

### 3.1 User roles
| Role | What this role can do in this feature |
|------|----------------------------------------|
| [Role] | ... |

### 3.2 Permissions
| Action | Allowed roles | Notes |
|--------|---------------|-------|
| ... | ... | ... |

---

## 4. Feature inventory (bill of materials)

List **every** deliverable part. An engineer uses this list to size the work.

| ID | Part name | Type (page / section / dialog / job / email / API / rule) | Short description | Priority (must / should / later) |
|----|-----------|-------------------------------------------------------------|-------------------|----------------------------------|
| P1 | ... | ... | ... | must |

---

## 5. User journeys

Describe the main paths in order. Include the happy path and the main failure paths.

### 5.1 Journey: [Name]
**Actor:** [Role]  
**Start:** [Where the user begins]  
**End:** [Successful result]

1. ...
2. ...
3. ...

**If something fails:**
- At step X, if [condition], then [what the user sees / what the system does].

### 5.2 Journey: [Name]
...

---

## 6. Screens and states

### 6.1 Screen map
| Screen ID | Name | Primary job | Entry points |
|-----------|------|-------------|--------------|
| S1 | ... | ... | ... |

### 6.2 UI regions (for complex pages)
| Page | Region | Content / controls | Notes |
|------|--------|--------------------|-------|
| ... | ... | ... | ... |

### 6.3 State matrix
| Screen / part | Empty | Loading | Success | Error |
|---------------|-------|---------|---------|-------|
| ... | ... | ... | ... | ... |

---

## 7. Glossary and module definitions

Define every ambiguous term. One meaning only. This section is mandatory for complex features.

### 7.1 Glossary
| Term | Plain definition | Example |
|------|------------------|---------|
| ... | ... | ... |

### 7.2 Module definitions
For each major module, fill this block.

#### Module: [Name]
- **One-sentence definition:** ...
- **Responsibility:** What this module does.
- **Not responsible for:** What this module must not do.
- **Inputs:** What it receives.
- **Outputs:** What it produces.
- **Depends on:** Other modules it needs.
- **Used by:** Other modules or screens that need it.

Repeat for each module.

---

## 8. Data model

### 8.1 Entities
| Entity | Plain meaning | Key fields | Owned by |
|--------|---------------|------------|----------|
| ... | ... | ... | ... |

### 8.2 Relationships
- [Entity A] has many [Entity B].
- ...

### 8.3 Field rules
| Entity.field | Required? | Allowed values / format | Notes |
|--------------|-----------|-------------------------|-------|
| ... | yes/no | ... | ... |

### 8.4 Stored vs temporary
| Data | Stored permanently? | Temporary where? | Why |
|------|---------------------|------------------|-----|
| ... | yes/no | session / memory / cache / none | ... |

---

## 9. Data flow

Explain how information moves for the main actions. Use short steps.

### 9.1 Flow: [Action name]
**Trigger:** [User action or system event]

1. [Source] sends [data] to [destination].
2. [Module] checks [rule].
3. [Module] writes / reads [entity].
4. [UI] shows [result].

**Failure path:** If [condition], then [result].

### 9.2 Flow: [Action name]
...

### 9.3 External systems
| System | Direction (in / out / both) | What is exchanged | When |
|--------|-----------------------------|-------------------|------|
| ... | ... | ... | ... |

---

## 10. Contracts between parts

Do **not** invent endpoint paths or payload shapes. If unknown, list an Open question.

### 10.1 Logical operations (required)
| Operation | Request (fields) | Response (fields) | Errors |
|-----------|------------------|-------------------|--------|
| ... | ... | ... | ... |

### 10.2 Events (if used)
| Event name | When it fires | Payload fields | Who listens |
|------------|---------------|----------------|-------------|
| ... | ... | ... | ... |

### 10.3 Shared UI contracts (if needed)
| Component / region | Required props / data | Emits / callbacks |
|--------------------|-----------------------|-------------------|
| ... | ... | ... |

---

## 11. Business rules

| ID | Rule | Example |
|----|------|---------|
| R1 | ... | ... |

---

## 12. Content and messaging

| Situation | Message to the user | Tone notes |
|-----------|---------------------|------------|
| Success | ... | ... |
| Validation error | ... | ... |
| System error | ... | ... |
| Empty state | ... | ... |

---

## 13. Analytics and observability (if needed)

| Event / signal | When | Properties | Why it matters |
|----------------|------|------------|----------------|
| ... | ... | ... | ... |

---

## 14. Edge cases and risks

| Case | Expected behavior | Severity |
|------|-------------------|----------|
| ... | ... | high/medium/low |

---

## 15. Open questions and decisions

| ID | Question | Options | Decision | Owner | Due date |
|----|----------|---------|----------|-------|----------|
| Q1 | ... | A / B | undecided | ... | ... |

---

## 16. Build order (suggested)

1. Agree glossary and data model.
2. Build core module contracts.
3. Build main data flows.
4. Build primary screen states.
5. Add edge cases and messaging.
6. Add analytics and final checks.

---

## 17. Acceptance checklist

- [ ] Scope is clear (in / out).
- [ ] Inventory covers all parts.
- [ ] Ambiguous terms have one definition each.
- [ ] Main journeys are written.
- [ ] Data model is complete enough to implement.
- [ ] Main data flows are written.
- [ ] Contracts between modules are clear (or listed as open questions).
- [ ] Edge cases and open questions are listed.
- [ ] Definition of done is testable.

---

## Who writes what

| Section | Non-technical author | Engineer |
|---------|----------------------|----------|
| 1 Purpose | Owns | Reviews |
| 2 Scope | Owns | Reviews; may tighten constraints |
| 3 Users and access | Owns role intent | Owns exact enforcement |
| 4 Inventory | Owns pages/dialogs/emails/states from Figma | Adds jobs/APIs/services |
| 5 Journeys | Owns user steps and failure outcomes | Adds system steps |
| 6 Screens and states | Owns | Reviews |
| 7.1 Glossary | Owns product words | Tightens meanings |
| 7.2 Modules | Does **not** invent service topology | Owns |
| 8 Data model | May list business objects in plain words | Owns fields, storage, derived vs stored |
| 9 Data flow | Owns **user-visible** outcomes (“file ready”, “show error”) | Owns mechanism (sync/async, poll/notify, which service) |
| 10 Contracts | Does **not** invent APIs or job protocols | Owns |
| 11 Business rules | Owns product rules | Confirms testability |
| 12 Content | Owns | Reviews |
| 13 Analytics | Drafts event names/why | Owns properties and privacy |
| 14 Edge cases | Owns user-facing cases | Adds concurrency/infra cases |
| 15 Open questions | Owns **product** decisions | Owns **technical** decisions; both listed |
| 16 Build order | Optional draft | Owns |

### Product may state outcomes. Product must not design internals.

**Good (product):**
- If export takes longer than a normal page load, the user must see a clear waiting state and must learn when the file is ready.
- Sensitive exports need acknowledgement, permission, and an audit record.
- Keep exports for [N days] / follow company retention policy (or mark as Open question).

**Bad (do not ask non-tech to write):**
- Async export job contract, poll vs notify, failure code enums.
- One Reports API vs per-domain read models.
- Which microservice owns each query.
- Exact HTTP paths, payload schemas, or warehouse vs OLTP choice.

Those belong in an **engineering companion** or in sections 7.2–10 filled by engineering after the product spec is accepted.

### Open questions — split by owner

In section 15, mark **Owner** as `Product` or `Engineering`.

| Owner | Example questions |
|-------|-------------------|
| Product | Which roles may export consent data? Manual brief only, or scheduled too? Which conversion formula is the product standard? |
| Engineering | Sync vs async export? Poll, email, or in-app notify? One Reports BFF vs domain queries? Persist PDF where? |

Non-technical authors are **not** incomplete if Engineering rows are blank. They **are** incomplete if Product rows that block UX or policy stay blank.

## How non-technical authors should use this

1. Fill sections 1–6 from product knowledge and Figma.
2. Fill section 7.1 (glossary) as soon as product words appear. Skip 7.2.
3. Draft section 12 (messages) from design copy.
4. For slow or sensitive actions, write **user outcomes** only (waiting, ready, failed, retry). Do not design jobs or APIs.
5. List **product** unknowns in section 15 with Owner = Product.
6. Leave Engineering open questions empty or tagged Owner = Engineering for the engineer to fill.
7. Ask an engineer to complete sections 7.2–10 (and Engineering rows in 15) before build estimate.
8. Do not start build while **Product** open questions that affect behaviour or policy are still blocking.

## Engineer readiness test

After one read of the **product** draft, an engineer must answer **yes** to:

1. What parts must ship (from inventory and screens)?
2. What does each ambiguous **product** term mean?
3. What user-visible states exist (empty, loading, error, success, waiting for long work)?
4. What are the hard **product** rules and permissions intent?
5. What **product** decisions are still open?

After the **engineering companion** (or sections 7.2–10) is filled, also answer **yes** to:

6. What is stored, temporary, or derived?
7. For each main action, what is the system data flow?
8. What are the contracts between parts?
