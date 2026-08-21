# Home Needs attention is a pooled projection

**Home Needs attention** is a now-queue on Operator Home: the **Operator Home page module** assembles **Home Needs attention item**s from existing source queues for the selected Owned location. It does not persist an attention status, an attention table, or a Home-owned membership engine. First-cut sources: **Feedback Needs attention** (one aggregate count), Campaigns Needs attention (one named row per Failed or Partially sent Campaign), and **Offers list Needs attention** (one named Offer; Void request wins over expiry). A later source is a low **AI credit**, **Email credit**, or **SMS credit** pool (account-scoped; Billing owns membership; Home row plus a **Notification**).

This differs from **Home Recommended next step**, which has its own `POST /api/home/recommendation` because that card is an AI next-action, not a queue projection. Live offers and campaigns already assemble Home cards from domain Offers and Campaigns reads; Needs attention follows that pattern, not the recommendation API.

We rejected a stored Home attention inbox (new rows, last-seen, Home-only dismiss) because membership already lives on Offers, Campaigns, and Feedback, and a second store would drift. We rejected a change-since-last-visit feed because **Weekly brief** owns the weekly summary and the Home subtitle is “issues that may require action” now. We rejected restaurant-wide location-owned rows (Figma “All locations”) because Home follows the location switcher; credit rows are the account-scoped exception when Billing metering is live.

## Consequences

- Do not add `GET /api/home/needs-attention` as a generate/store job. Assembly may compose existing Offers, Campaigns, and Feedback reads in the Home module (same idea as Live offers). A thin shaping endpoint is allowed only if it does not own membership.
- Rows leave when the source queue drops them. No Home-only dismiss.
- Offers, Campaigns, and Feedback keep their own Needs attention queues. Home is an extra window, cap 5, then View all expands in the accordion.
- Future “give Home its own attention store / AI attention pipeline” work should supersede this ADR explicitly.
