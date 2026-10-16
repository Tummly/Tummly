import { MARKETING_DEMO_PATH } from "@/constants/marketingNav"
import type { ProductDemoTourStepId } from "@/content/marketing/productDemoPage"
import type { OperatorSidebarActiveId } from "@/types/operatorHome"

export const PRODUCT_DEMO_BASE = MARKETING_DEMO_PATH
export const PRODUCT_DEMO_DASHBOARD_BASE = "/single-dashboard"

export type ProductDemoPaneId =
  | "home"
  | "feedback"
  | "guests"
  | "offers"
  | "reports"

export function productDemoPath(pane: ProductDemoPaneId = "home"): string {
  if (pane === "home") {
    return PRODUCT_DEMO_DASHBOARD_BASE
  }
  return `${PRODUCT_DEMO_DASHBOARD_BASE}/${pane}`
}

export function productDemoPaneFromPathname(
  pathname: string
): ProductDemoPaneId {
  const base = PRODUCT_DEMO_DASHBOARD_BASE
  if (pathname === base || pathname === `${base}/`) {
    return "home"
  }
  if (pathname.startsWith(`${base}/feedback`)) return "feedback"
  if (pathname.startsWith(`${base}/guests`)) return "guests"
  if (pathname.startsWith(`${base}/offers`)) return "offers"
  if (pathname.startsWith(`${base}/reports`)) return "reports"
  return "home"
}

export function productDemoActiveNavId(
  pane: ProductDemoPaneId
): OperatorSidebarActiveId {
  return pane
}

export function productDemoPathForTourStep(
  stepId: ProductDemoTourStepId
): string {
  return productDemoPath(stepId)
}

/** Entry route only (`/product-demo`) — tour runs on `/single-dashboard`. */
export function isProductDemoEntryPath(pathname: string): boolean {
  return (
    pathname === PRODUCT_DEMO_BASE || pathname.startsWith(`${PRODUCT_DEMO_BASE}/`)
  )
}

/** @deprecated Prefer isProductDemoEntryPath — kept for MainLayout. */
export function isProductDemoPath(pathname: string): boolean {
  return isProductDemoEntryPath(pathname)
}
