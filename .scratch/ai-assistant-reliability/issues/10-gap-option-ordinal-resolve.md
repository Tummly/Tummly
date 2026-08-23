# 10 — Gap option ordinal resolve

**Parent:** `.scratch/ai-assistant-reliability/PRD.md`. Map tickets 01–09 are the decision record. If this ticket and a map ticket disagree, the PRD wins.

**What to build:** On an open Gap `Options` list, the operator can choose with a whole-message ordinal (`1`, `number 1`, `option 1`, `first`, `the first one`, `last`, `the last one`) without repeating the label. A named choice still wins. A normal fill such as `14 days` or `10% off` is not treated as an option index. An out-of-range ordinal repeats the Gap. Recovery feedback `number 1` / `option 1` stay misses. Location unique-name resolve stays non-ordinal. An unnamed create Gap then `number 1` starts Campaign (first unnamed option), not Recovery.

**Blocked by:** None — can start immediately.

**Status:** resolved  
**Type:** task

- [x] Ordinal fallback exists only on the two existing Gap option resolvers (create-target resolve and Campaign named-choice resolve). No third choice engine. Resolve order stays exact/name, then Detect/phrase, then ordinal only if still unbound. Name wins: `Campaign` binds Campaign even when it is option 2.
- [x] After the same trim and punctuation clean as resolve today, the entire message must be an ordinal. Do not extract digits from a longer fill. Accept: `1` / `2` / …; `number N`; `option N`; `first` / `second` / `third` / …; `the first one` / `the second one` / …; `last` / `the last one`. Index is 1-based into stored Options in Join order. Do not treat `latest` / `most recent` as Gap ordinals.
- [x] Out-of-range ordinal is a miss: the Gap repeats. `14 days` and `10% off` do not bind as ordinals. `Yes, proceed with number 1` is a miss (whole-message rule). Fixture `number 1` binds. Do not strip leading “Yes, proceed with” in product code.
- [x] Recovery feedback resolve is unchanged: `number 1` / `option 1` stay Recovery misses. Location unique-name resolve does not gain ordinals. Gap ask copy does not change.
- [x] SendTurn: unnamed create Gap (`help me draft something`) then `number 1` / `1` / `first` / `option 1` / `the first one` starts Campaign (or a Campaign Gap). Not Recovery. Pending Recovery payload is null. Catalog row count stays 0 if Campaign is not yet complete. Offer-title Gap: `number 1` binds the first stored option.
- [x] Tests cover both resolvers for every ordinal row above, plus the SendTurn unnamed-create cases. Assert structured bind / Gap JSON / task, not reply prose. Reliability-doc strings are test inputs only.

## Answer

Shipped on `feat/ai-assistant-reliability` (`AssistantGapOptionOrdinal.TryBind` called only from `AssistantCreateTargets.Resolve` and `AssistantCampaignDraftBind.ResolveNamedChoice`). Whole-message ordinals; name / Detect / phrase first; Recovery and location unique-name stay non-ordinal. SendTurn coverage includes unnamed-create first-option ordinals, offer-title `number 1`, and out-of-range Gap repeat.
