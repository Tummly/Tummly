# Workspace selection lists Active memberships

[Workspace selection is per-restaurant](./0002-workspace-selection-is-per-restaurant.md) stays: the operator picks a Restaurant, not an **Owned location**. The list is **Active** **Restaurant membership**s for that User, not `Restaurant.OwnerUserId` / owned restaurants only. A User may be **Staff** (or any non-owner **permission role**) at a Restaurant they do not own, and may hold memberships in more than one Restaurant. **Deactivated** memberships are omitted.

We rejected storing the Restaurant **permission role** on `User.Role`: that field is one string per User and cannot represent two memberships or a per-Restaurant **Deactivate**.
