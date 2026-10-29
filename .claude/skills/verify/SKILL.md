---
name: verify
description: End-to-end verify a built feature in a real browser against its spec and Figma frame, fix failures, and save passing flows as Playwright tests.
disable-model-invocation: true
argument-hint: <spec path> <Figma frame URL with node-id>
---

Input: `$ARGUMENTS` — a spec path and one or more Figma frame URLs. Run facts (dev command, URL, login, breakpoints, test command, artifact folder) live in `CLAUDE.md` → **Verify setup**. A missing spec or Figma URL → ask the user before step 1.

Every check ends **pass** or **fail** on evidence: a screenshot path, a passing test, a DOM excerpt, or a computed-style value. A check without evidence is **fail**.

1. **Checklist.** Read the spec and the tickets it links. Open each Figma frame through Figma MCP (`get_design_context`, `get_screenshot`; load `/figma-design-to-code` first). Write a numbered list: **F1…** functional checks (every user flow plus its empty, error, and loading states) and **V1…** visual checks (per frame and breakpoint). Done when every acceptance criterion and every frame maps to at least one check.
2. **App.** Probe the dev URL, the API, and the T3 CDP port (**Browser** in `CLAUDE.md`); start the dev command or API in the background only when it does not answer. Open the dev URL in the T3 browser.
3. **Flows.** Drive each F-check with Chrome DevTools MCP on that T3 tab. Reach empty, error, and loading states with real data, `emulate` (slow or offline network), or a stopped API. Record the evidence per check.
4. **Errors.** On the same tab, list console errors and failed network requests (status ≥ 400 or failed). Each one is a fail on the check whose flow raised it.
5. **Screenshots.** At each breakpoint, screenshot every screen and state into the artifact folder.
6. **Design diff.** For each V-check, read the browser's computed styles (`getComputedStyle`, bounding boxes) for spacing, color, typography, radius, and layout. Compare them with the Figma values. Write each difference as `element · property · expected (Figma) · actual (browser)`. A difference is a fail unless a design token or `docs/agents/typography.md` explains it.
7. **Fix loop.** Fix each fail in the code, then run steps 3–6 again on the failed checks and the checks those fixes touch. Stop after round 3 and carry the remaining fails to the report.
8. **Tests.** Save each passed flow as `e2e/<feature-slug>-<flow>.spec.ts`, using role and label locators and the `.env` login. Run the test command from `CLAUDE.md`. Done when the suite is green or each red test has a fail row in the report.
9. **Report.** One table: `# · check · pass/fail · evidence`. Then the open differences, the rounds used, and the files changed. Say "done" only when every row is pass with evidence.
