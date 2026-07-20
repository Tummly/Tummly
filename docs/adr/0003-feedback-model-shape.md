# Feedback model shape

Guest feedback submitted via the Smart Guest Link is stored per-location, attributed to the `RestaurantLocation` whose token was used to reach the form. The `Feedback` table has no `RestaurantId` column — the restaurant is reached via `RestaurantLocation.RestaurantId` when needed.

The form captures three required text fields: guest name, guest contact (a single string holding either an email or a phone number, guest's choice), and a comment. It also captures `OffersOptOut`, a boolean that defaults to `false`; the pre-checked UI is mapped so unticking records `true`. A `ContactType` column (`Email` | `Phone` | `Unknown`) records a heuristic classification of the contact string at submission time, so future operator-reply features know which channel to use without re-parsing.

We rejected separate `GuestEmail` and `GuestPhone` columns because the form has a single contact field and forcing the guest to choose a channel adds friction. We rejected a per-restaurant FK because the Smart Guest Link is per-location — attribution must follow the token, not the restaurant — and per-location feedback is what the per-location dashboard needs to display.
