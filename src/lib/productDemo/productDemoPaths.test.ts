import { describe, expect, it } from "vitest"

import {
  isProductDemoPath,
  productDemoPaneFromPathname,
  productDemoPath,
  productDemoPathForTourStep,
} from "@/lib/productDemo/productDemoPaths"

describe("productDemoPaths", () => {
  it("builds real single-dashboard paths for the tour", () => {
    expect(productDemoPath("home")).toBe("/single-dashboard")
    expect(productDemoPath("feedback")).toBe("/single-dashboard/feedback")
    expect(productDemoPathForTourStep("offers")).toBe(
      "/single-dashboard/offers"
    )
  })

  it("resolves pane and entry-path membership", () => {
    expect(productDemoPaneFromPathname("/single-dashboard")).toBe("home")
    expect(productDemoPaneFromPathname("/single-dashboard/guests")).toBe(
      "guests"
    )
    expect(isProductDemoPath("/product-demo")).toBe(true)
    expect(isProductDemoPath("/single-dashboard")).toBe(false)
  })
})
