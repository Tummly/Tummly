# Needs recovery overview KPI uses Feedback submission time

**Guest overview** scopes **Total guests**, **New this month**, and **Marketing eligible** by Location Guest first-captured when a non–All-time **Guest overview date range** is selected. **Needs recovery** is the deliberate exception: under a window, its KPI counts distinct **Location Guests** that have ≥1 **Feedback** whose **Succeeded** classification sentiment is currently Negative and whose **submission time** (`Feedback.CreatedAt`) falls in that window — even when first-captured is outside the window. All time still equals Needs recovery Smart Group membership count in location scope. Smart Group membership itself stays independent of the overview control (any current Succeeded Negative).

We rejected applying first-captured cohort scoping to Needs recovery (consistent strip, but hides recent Negatives from long-lived guests). We rejected rewriting all four KPIs onto event-time axes in the same cut. We rejected a Filter-Sheet-style overview Date axis picker. Classification-Succeeded time was rejected as the window clock (no persisted `ClassifiedAt`; submission time already owns Feedback freshness elsewhere).

## Consequences

- Guests list overview assembly must not run Needs recovery through `ApplyCapturedAtWindow`; the special case should stay obvious in code and tests.
- Operators can see Last 7 days Needs recovery KPI ≠ Needs recovery tab count; that mismatch is expected until a later overview redesign or recovery analytics surface.
- Future “fix overview so every KPI shares one date meaning” work should treat this ADR as the reason Needs recovery diverges today — supersede explicitly rather than silently reverting.
