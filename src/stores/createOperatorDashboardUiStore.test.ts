import { describe, expect, it } from "vitest"

import { createOperatorDashboardUiStore } from "@/stores/createOperatorDashboardUiStore"
import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"

describe("createOperatorDashboardUiStore", () => {
  it("defaults homePerformanceDateRange to Last 7 days", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().homePerformanceDateRange).toEqual(
      DEFAULT_HOME_PERFORMANCE_DATE_RANGE
    )
  })

  it("commits a preset selection without persist middleware", () => {
    const store = createOperatorDashboardUiStore()
    store.getState().setHomePerformanceDateRange({
      kind: "preset",
      presetId: "thisMonth",
    })
    expect(store.getState().homePerformanceDateRange).toEqual({
      kind: "preset",
      presetId: "thisMonth",
    })
  })
})
