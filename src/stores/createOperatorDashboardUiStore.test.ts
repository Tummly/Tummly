import { describe, expect, it } from "vitest"

import {
  createOperatorDashboardUiStore,
  DEFAULT_CAPTURE_PERFORMANCE_DATE_RANGE,
} from "@/stores/createOperatorDashboardUiStore"
import { DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
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

  it("defaults multiCaptureOverviewDateRange to Last 7 days independently", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().multiCaptureOverviewDateRange).toEqual(
      DEFAULT_HOME_PERFORMANCE_DATE_RANGE
    )
    store.getState().setCapturePerformanceDateRange({
      kind: "preset",
      presetId: "last30",
    })
    expect(store.getState().multiCaptureOverviewDateRange).toEqual(
      DEFAULT_HOME_PERFORMANCE_DATE_RANGE
    )
  })

  it("commits a Multi Capture overview range without rewriting Capture performance", () => {
    const store = createOperatorDashboardUiStore()
    store.getState().setMultiCaptureOverviewDateRange({
      kind: "preset",
      presetId: "thisMonth",
    })
    expect(store.getState().multiCaptureOverviewDateRange).toEqual({
      kind: "preset",
      presetId: "thisMonth",
    })
    expect(store.getState().capturePerformanceDateRange).toEqual(
      DEFAULT_CAPTURE_PERFORMANCE_DATE_RANGE
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

  it("defaults campaignsOverviewDateRange to Last 30 days independently of Guests", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().campaignsOverviewDateRange).toEqual(
      DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE
    )
    expect(store.getState().guestsOverviewDateRange).toEqual(
      DEFAULT_GUESTS_OVERVIEW_DATE_RANGE
    )
    store.getState().setGuestsOverviewDateRange({
      kind: "preset",
      presetId: "last7",
    })
    expect(store.getState().campaignsOverviewDateRange).toEqual(
      DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE
    )
  })

  it("commits a Campaigns overview All time / preset without rewriting Guests", () => {
    const store = createOperatorDashboardUiStore()
    store.getState().setCampaignsOverviewDateRange({ kind: "all-time" })
    expect(store.getState().campaignsOverviewDateRange).toEqual({
      kind: "all-time",
    })
    expect(store.getState().guestsOverviewDateRange).toEqual(
      DEFAULT_GUESTS_OVERVIEW_DATE_RANGE
    )
    store.getState().setCampaignsOverviewDateRange({
      kind: "preset",
      presetId: "thisMonth",
    })
    expect(store.getState().campaignsOverviewDateRange).toEqual({
      kind: "preset",
      presetId: "thisMonth",
    })
  })

  it("holds a Feedback inbox intent until the Feedback page consumes it", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().feedbackInboxIntent).toBeNull()
    store.getState().setFeedbackInboxIntent({ tab: "needs-attention" })
    expect(store.getState().feedbackInboxIntent).toEqual({
      tab: "needs-attention",
    })
    store.getState().setFeedbackInboxIntent(null)
    expect(store.getState().feedbackInboxIntent).toBeNull()
  })

  it("holds a Campaigns Drafts intent until Campaigns consumes it", () => {
    const store = createOperatorDashboardUiStore()
    store.getState().setCampaignsIntent({ view: "drafts" })
    expect(store.getState().campaignsIntent).toEqual({ view: "drafts" })
    store.getState().setCampaignsIntent(null)
    expect(store.getState().campaignsIntent).toBeNull()
  })

  it("holds an Offers Drafts intent until Offers consumes it", () => {
    const store = createOperatorDashboardUiStore()
    store.getState().setOffersIntent({ view: "drafts" })
    expect(store.getState().offersIntent).toEqual({ view: "drafts" })
    store.getState().setOffersIntent(null)
    expect(store.getState().offersIntent).toBeNull()
  })

  it("holds a Guests intent until the Guests page consumes it", () => {
    const store = createOperatorDashboardUiStore()
    expect(store.getState().guestsIntent).toBeNull()
    store.getState().setGuestsIntent({ marketingEligible: true })
    expect(store.getState().guestsIntent).toEqual({
      marketingEligible: true,
    })
    store.getState().setGuestsIntent(null)
    expect(store.getState().guestsIntent).toBeNull()
  })
})
