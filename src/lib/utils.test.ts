import { describe, expect, it } from "vitest"

import { cn } from "./utils"

describe("cn / twMerge operator text tokens", () => {
  it("keeps Operator font-size utilities alongside Operator text colours", () => {
    expect(cn("text-op-xs font-semibold", "text-op-kpi-info-color")).toBe(
      "text-op-xs font-semibold text-op-kpi-info-color"
    )
    expect(
      cn("text-op-kpi-info-size font-semibold", "text-op-kpi-info-color")
    ).toBe("text-op-kpi-info-size font-semibold text-op-kpi-info-color")
    expect(cn("text-op-sm font-medium", "text-op-kpi-label-color")).toBe(
      "text-op-sm font-medium text-op-kpi-label-color"
    )
    expect(cn("text-op-xl font-extrabold", "text-op-kpi-value-color")).toBe(
      "text-op-xl font-extrabold text-op-kpi-value-color"
    )
  })

  it("still merges conflicting Operator font sizes to the last one", () => {
    expect(cn("text-op-xs", "text-op-sm")).toBe("text-op-sm")
  })
})

describe("cn / twMerge operator radius tokens", () => {
  it("lets Operator button radius replace the base pill radius", () => {
    expect(cn("rounded-full", "rounded-op-sm")).toBe("rounded-op-sm")
    expect(cn("rounded-full rounded-op-sm")).toBe("rounded-op-sm")
  })

  it("merges conflicting Operator radii to the last one", () => {
    expect(cn("rounded-op-sm", "rounded-op-lg")).toBe("rounded-op-lg")
    expect(cn("rounded-op-md", "rounded-xs")).toBe("rounded-xs")
  })
})
