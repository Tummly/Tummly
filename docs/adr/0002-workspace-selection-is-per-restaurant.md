# Workspace selection is per-restaurant, not per-location

Workspace selection — the post-authentication step where an operator chooses which restaurant to work in — is keyed by Restaurant, not by RestaurantLocation. A multi-restaurant operator sees a list of their restaurants at sign-in and picks one. A single-restaurant operator skips this step entirely and lands directly on their dashboard, regardless of how many locations that restaurant has.

We rejected location-keyed workspace selection because conflating "which restaurant am I working in" with "which location am I looking at" collapses two distinct concepts. An operator manages a restaurant; they switch between that restaurant's locations inside the dashboard via an in-dashboard location switcher, not at the sign-in gate.

Today every operator owns one restaurant, so workspace selection is dormant — the frontend UI and DTOs exist but the backend never sends `workspaceSetupRequired`. The `SelectWorkspaceDto` currently holds a `LocationId`; it will need to hold a `RestaurantId` when multi-restaurant ownership is introduced. This ADR records that the future shape is restaurant-keyed, so the current location-keyed DTO is a placeholder to be replaced, not a design to be built upon.
