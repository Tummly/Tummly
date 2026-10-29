import { afterEach, describe, expect, it, vi } from "vitest"

import { isProductDemoEnabled } from "@/lib/env"

describe("isProductDemoEnabled", () => {
  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it("is off unless VITE_PRODUCT_DEMO_ENABLED is exactly \"true\"", () => {
    vi.stubEnv("VITE_PRODUCT_DEMO_ENABLED", "")
    expect(isProductDemoEnabled()).toBe(false)

    vi.stubEnv("VITE_PRODUCT_DEMO_ENABLED", "false")
    expect(isProductDemoEnabled()).toBe(false)

    vi.stubEnv("VITE_PRODUCT_DEMO_ENABLED", "TRUE")
    expect(isProductDemoEnabled()).toBe(false)

    vi.stubEnv("VITE_PRODUCT_DEMO_ENABLED", "1")
    expect(isProductDemoEnabled()).toBe(false)
  })

  it("is on when VITE_PRODUCT_DEMO_ENABLED is \"true\"", () => {
    vi.stubEnv("VITE_PRODUCT_DEMO_ENABLED", "true")
    expect(isProductDemoEnabled()).toBe(true)
  })
})
