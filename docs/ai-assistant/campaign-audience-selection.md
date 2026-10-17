# Campaign audience selection

Campaign Drafts can refer to a stored audience with an `audience:<id>` key.
The rule is stored at one location. Another location cannot read or use it.
The rule does not grant marketing consent.

## Chat use

Create a Campaign Draft, then use these requests:

- `Send to Mohamed and Sophie only` (use exact guest names).
- `Change the audience to the 10 most recent eligible guests`.
- `Change the audience to all eligible guests excluding Sophie`.
- `Draft an Email Campaign for guests who joined in the last 30 days`.
- `Save this audience as Recent visitors`.
- `List saved audiences`.
- `Use saved audience Recent visitors`.

Missing or duplicate names require a specific guest selection. They do not
fall back to all guests. A count without a selection rule requires a question.
An exclusion-only change to a stored audience keeps its other restrictions.

Saved audiences are available on all plans. Campaign access and billing locks
still apply. Saving an audience does not send or schedule a Campaign.

## API

`POST /api/campaign-audiences` accepts `locationId`, an optional saved `name`,
and a `definition`. `GET /api/campaign-audiences?locationId=...` lists saved
audiences. Both routes require location access; POST requires Manage access.

Supported rule fields:

- `baseAudienceKey`: an existing Campaign preset.
- `guestIds` / `excludedGuestIds`: location guest IDs.
- `guestNames` / `excludedNames`: exact names, resolved to IDs at creation.
- `limit`: 1 to 10,000 final channel-eligible recipients.
- `order`: `newest` (join date) or `most-recent` (recorded activity, with join
  date as the fallback). ID breaks equal-date ties.
- `joinedFromUtc` / `joinedToUtc`: inclusive start, exclusive end.
- `nameContains`: a name filter.

The response contains the stored key, resolved definition, matched count, and
Email/SMS eligible counts. Use that key as the Draft `audienceKey`.
Rules are immutable. Save a new rule to change a saved audience.
An unsupported preset stays unavailable; it does not become all eligible.

Channel permission checks run before the limit. Schedule commit and fire use
the same selection service and recheck permission. Fire retains the existing
frozen-recipient rule: it must not add recipients who were not frozen at commit.

## Database

Deploy both generated `AddCampaignAudienceDefinitions` and
`AddCampaignAudienceScopeConstraints` migrations before the API update.
The local database has these migrations. No Live migration was run.
