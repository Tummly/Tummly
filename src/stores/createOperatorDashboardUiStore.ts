import { createStore, type StoreApi } from "zustand/vanilla"

import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  DEFAULT_GUESTS_OVERVIEW_DATE_RANGE,
  type GuestsOverviewDateRange,
} from "@/lib/operatorGuests/guestsOverviewDateRange"

export type OperatorDashboardUiState = {
  homePerformanceDateRange: HomePerformanceDateRange
  setHomePerformanceDateRange: (range: HomePerformanceDateRange) => void
  guestsOverviewDateRange: GuestsOverviewDateRange
  setGuestsOverviewDateRange: (range: GuestsOverviewDateRange) => void
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
  }))
}
