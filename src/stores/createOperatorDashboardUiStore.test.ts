import { describe, expect, it } from "vitest"

import {
  createOperatorDashboardUiStore,
  DEFAULT_CAPTURE_PERFORMANCE_DATE_RANGE,
} from "@/stores/createOperatorDashboardUiStore"
import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"
import { DEFAULT_GUESTS_OVERVIEW_DATE_RANGE } from "@/lib/operatorGuests/guestsOverviewDateRange"

describe("createOperatorDashboardUiStore", () => {
  it("defaults homePerformanceDateRange to Last 7 days", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().homePerformanceDateRange).toEqual(
      DEFAULT_HOME_PERFORMANCE_DATE_RANGE
    )
  })

  it("defaults guestsOverviewDateRange to All time", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().guestsOverviewDateRange).toEqual(
      DEFAULT_GUESTS_OVERVIEW_DATE_RANGE
    )
  })

  it("defaults capturePerformanceDateRange to Last 7 days independently", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().capturePerformanceDateRange).toEqual(
      DEFAULT_CAPTURE_PERFORMANCE_DATE_RANGE
    )
    store.getState().setHomePerformanceDateRange({
      kind: "preset",
      presetId: "last30",
    })
    expect(store.getState().capturePerformanceDateRange).toEqual(
      DEFAULT_CAPTURE_PERFORMANCE_DATE_RANGE
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

  it("commits a Capture performance range without rewriting Home", () => {
    const store = createOperatorDashboardUiStore()
    store.getState().setCapturePerformanceDateRange({
      kind: "preset",
      presetId: "thisMonth",
    })
    expect(store.getState().capturePerformanceDateRange).toEqual({
      kind: "preset",
      presetId: "thisMonth",
    })
    expect(store.getState().homePerformanceDateRange).toEqual(
      DEFAULT_HOME_PERFORMANCE_DATE_RANGE
    )
  })

  it("commits a Guests overview All time / preset without persist middleware", () => {
    const store = createOperatorDashboardUiStore()
    store.getState().setGuestsOverviewDateRange({
      kind: "preset",
      presetId: "last30",
    })
    expect(store.getState().guestsOverviewDateRange).toEqual({
      kind: "preset",
      presetId: "last30",
    })
    store.getState().setGuestsOverviewDateRange({ kind: "all-time" })
    expect(store.getState().guestsOverviewDateRange).toEqual({
      kind: "all-time",
    })
  })
})
