import { createStore, type StoreApi } from "zustand/vanilla"

import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  DEFAULT_GUESTS_OVERVIEW_DATE_RANGE,
  type GuestsOverviewDateRange,
} from "@/lib/operatorGuests/guestsOverviewDateRange"

/** Capture performance date range — same shape as Home; separate visit-scoped value. */
export type CapturePerformanceDateRange = HomePerformanceDateRange

/** Multi Capture overview date range — same shape as Home; separate visit-scoped value. */
export type MultiCaptureOverviewDateRange = HomePerformanceDateRange

/** Feedback page date range — same shape as Home; drives summary + inbox. */
export type FeedbackPageDateRange = HomePerformanceDateRange

export const DEFAULT_CAPTURE_PERFORMANCE_DATE_RANGE: CapturePerformanceDateRange =
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE

export const DEFAULT_MULTI_CAPTURE_OVERVIEW_DATE_RANGE: MultiCaptureOverviewDateRange =
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE

export const DEFAULT_FEEDBACK_PAGE_DATE_RANGE: FeedbackPageDateRange =
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE

export type OperatorDashboardUiState = {
  homePerformanceDateRange: HomePerformanceDateRange
  setHomePerformanceDateRange: (range: HomePerformanceDateRange) => void
  guestsOverviewDateRange: GuestsOverviewDateRange
  setGuestsOverviewDateRange: (range: GuestsOverviewDateRange) => void
  capturePerformanceDateRange: CapturePerformanceDateRange
  setCapturePerformanceDateRange: (range: CapturePerformanceDateRange) => void
  multiCaptureOverviewDateRange: MultiCaptureOverviewDateRange
  setMultiCaptureOverviewDateRange: (
    range: MultiCaptureOverviewDateRange
  ) => void
  feedbackPageDateRange: FeedbackPageDateRange
  setFeedbackPageDateRange: (range: FeedbackPageDateRange) => void
}

export type OperatorDashboardUiStore = StoreApi<OperatorDashboardUiState>

/** Visit-scoped store factory — one instance per Operator Dashboard mount. */
export function createOperatorDashboardUiStore(): OperatorDashboardUiStore {
  return createStore<OperatorDashboardUiState>((set) => ({
    homePerformanceDateRange: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
    setHomePerformanceDateRange: (homePerformanceDateRange) =>
      set({ homePerformanceDateRange }),
    guestsOverviewDateRange: DEFAULT_GUESTS_OVERVIEW_DATE_RANGE,
    setGuestsOverviewDateRange: (guestsOverviewDateRange) =>
      set({ guestsOverviewDateRange }),
    capturePerformanceDateRange: DEFAULT_CAPTURE_PERFORMANCE_DATE_RANGE,
    setCapturePerformanceDateRange: (capturePerformanceDateRange) =>
      set({ capturePerformanceDateRange }),
    multiCaptureOverviewDateRange: DEFAULT_MULTI_CAPTURE_OVERVIEW_DATE_RANGE,
    setMultiCaptureOverviewDateRange: (multiCaptureOverviewDateRange) =>
      set({ multiCaptureOverviewDateRange }),
    feedbackPageDateRange: DEFAULT_FEEDBACK_PAGE_DATE_RANGE,
    setFeedbackPageDateRange: (feedbackPageDateRange) =>
      set({ feedbackPageDateRange }),
  }))
}
