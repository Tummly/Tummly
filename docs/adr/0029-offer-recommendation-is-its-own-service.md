# Offer recommendation is its own service

**Offer recommendation** on Offer Details is a dedicated generate path, not a wrapper around **Campaign recommendation**. Rules pick `promote-this-offer`, `fix-this-offer`, or `none` for **this** catalog offer. Azure writes copy (and promote message/channel) and cannot change the type. Promote `draftPrefill` requires `offerId` (this offer), `offerStance: "existing-offer"`, `goalId: "promote-something-new"`, and `audienceKey: "all-eligible-guests"`. Azure may set channel `email` or `sms`. `fix-this-offer` with an open Void switches to the Void requests tab only. Campaign recommendation stays location-wide (thank / re-engage / recovery) and may attach a different offer or none.

We rejected calling Campaign recommendation and overwriting the attached offer: that would keep a campaign type that is not about this offer. We rejected merging the two allow-lists: Offer Details must not show thank / re-engage / recovery.

HTTP: `POST /api/offers/{offerId}/recommendation`. Home (`POST /api/home/recommendation`) and Campaigns (`POST /api/campaigns/recommendation`) stay unchanged. This card does not ship **Offers Recommended next step**. Promote does not use Campaign audience **Offer not redeemed** in this slice (unevaluable and not scoped to this offer).
