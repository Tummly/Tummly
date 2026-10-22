import { describe, expect, it } from "vitest"

import {
  buildProductDemoGuestScanPath,
  isProductDemoPath,
  productDemoPaneFromPathname,
  productDemoPath,
  productDemoPathForChapter,
  productDemoPathForTourStep,
} from "@/lib/productDemo/productDemoPaths"

describe("productDemoPaths", () => {
  it("builds real single-dashboard paths for the tour", () => {
    expect(productDemoPath("home")).toBe("/single-dashboard")
    expect(productDemoPath("feedback")).toBe("/single-dashboard/feedback")
    expect(productDemoPathForChapter("offers")).toBe(
      "/single-dashboard/offers"
    )
    // Chapter 1 starts on Home: step 1 points at the Feedback nav item.
    expect(productDemoPathForChapter("feedback")).toBe("/single-dashboard")
    expect(productDemoPathForTourStep("ch1-s1")).toBe("/single-dashboard")
    expect(productDemoPath("campaigns")).toBe("/single-dashboard/campaigns")
  })

  it("resolves pane and entry-path membership", () => {
    expect(productDemoPaneFromPathname("/single-dashboard")).toBe("home")
    expect(productDemoPaneFromPathname("/single-dashboard/guests")).toBe(
      "guests"
    )
    expect(isProductDemoPath("/product-demo")).toBe(true)
    expect(isProductDemoPath("/single-dashboard")).toBe(false)
    expect(buildProductDemoGuestScanPath("demo-mavero-table-tent")).toBe(
      "/scan/demo-mavero-table-tent?productDemo=1"
    )
  })
})
