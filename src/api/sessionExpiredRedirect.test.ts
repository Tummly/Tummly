import { afterEach, describe, expect, it } from "vitest"

import { sessionExpiredRedirectPath } from "@/api/sessionExpiredRedirect"
import {
  clearDemoTourSession,
  isDemoTourActive,
  startDemoTourSession,
} from "@/lib/productDemo/demoTourSession"
import { PRODUCT_DEMO_BASE } from "@/lib/productDemo/productDemoPaths"

// A public product-demo session has no refresh token (server sandbox). When
// its access token expires, the visitor restarts the Demo, not operator login.
describe("sessionExpiredRedirectPath", () => {
  afterEach(() => {
    clearDemoTourSession()
  })

  it("sends an operator to login", () => {
    expect(sessionExpiredRedirectPath()).toBe("/login")
  })

  it("sends a demo visitor back to the product demo and ends the tour", () => {
    startDemoTourSession({ entryPath: "skip", phase: "entry" })

    expect(sessionExpiredRedirectPath()).toBe(PRODUCT_DEMO_BASE)
    expect(isDemoTourActive()).toBe(false)
  })
})
