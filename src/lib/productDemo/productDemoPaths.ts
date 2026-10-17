import { MARKETING_DEMO_PATH } from "@/constants/marketingNav"
import {
  PRODUCT_DEMO,
  type ProductDemoChapterId,
} from "@/content/marketing/productDemoPage"
import type { OperatorSidebarActiveId } from "@/types/operatorHome"

export const PRODUCT_DEMO_BASE = MARKETING_DEMO_PATH
export const PRODUCT_DEMO_DASHBOARD_BASE = "/single-dashboard"
export const PRODUCT_DEMO_QUERY = "productDemo=1"

export type ProductDemoPaneId =
  | "home"
  | "feedback"
  | "guests"
  | "offers"
  | "campaigns"
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
  if (pathname.startsWith(`${base}/campaigns`)) return "campaigns"
  if (pathname.startsWith(`${base}/reports`)) return "reports"
  return "home"
}

export function productDemoActiveNavId(
  pane: ProductDemoPaneId
): OperatorSidebarActiveId {
  if (pane === "campaigns") return "campaigns"
  return pane
}

export function productDemoPathForChapter(
  chapterId: ProductDemoChapterId
): string {
  const chapter = PRODUCT_DEMO.tour.chapters.find((c) => c.id === chapterId)
  const route = chapter?.route ?? "feedback"
  return productDemoPath(route)
}

/** @deprecated Use productDemoPathForChapter — kept for older call sites. */
export function productDemoPathForTourStep(
  stepId: string
): string {
  for (const chapter of PRODUCT_DEMO.tour.chapters) {
    if (chapter.steps.some((s) => s.id === stepId) || chapter.id === stepId) {
      return productDemoPath(chapter.route)
    }
  }
  return productDemoPath("feedback")
}

export function isProductDemoEntryPath(pathname: string): boolean {
  return (
    pathname === PRODUCT_DEMO_BASE || pathname.startsWith(`${PRODUCT_DEMO_BASE}/`)
  )
}

/** @deprecated Prefer isProductDemoEntryPath — kept for MainLayout. */
export function isProductDemoPath(pathname: string): boolean {
  return isProductDemoEntryPath(pathname)
}

export function isProductDemoGuestScanPath(pathname: string): boolean {
  return pathname.startsWith("/scan/")
}

export function buildProductDemoGuestScanPath(scanToken: string): string {
  return `/scan/${encodeURIComponent(scanToken)}?${PRODUCT_DEMO_QUERY}`
}
