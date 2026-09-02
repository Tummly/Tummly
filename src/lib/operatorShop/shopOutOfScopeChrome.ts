/**
 * Shop v1 chrome contract (ticket 21).
 * Drafts, Create QR asset, and seeded local orders are out of scope.
 */

export const SHOP_ORDERS_SURFACE_TABS = ["orders"] as const

export const SHOP_TOOLBAR_PRIMARY_ACTIONS = [
  { id: "view-orders", label: "View orders" },
] as const

export type ShopToolbarPrimaryActionId =
  (typeof SHOP_TOOLBAR_PRIMARY_ACTIONS)[number]["id"]
