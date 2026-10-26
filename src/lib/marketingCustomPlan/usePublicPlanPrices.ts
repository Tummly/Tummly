import { useEffect, useSyncExternalStore } from "react"

import { fetchPublicCustomUnitRates } from "@/api/adminCommercialCatalogApi"
import {
  toPublicPlanPrices,
  type PublicPlanPrices,
} from "@/lib/marketingCustomPlan/publicPlanPrices"

export type PublicPlanPricesStatus = "loading" | "ready" | "failed"

export type PublicPlanPricesState = {
  status: PublicPlanPricesStatus
  prices: PublicPlanPrices
  /** Sends a new catalogue request after a failure. */
  retry: () => void
}

type Snapshot = { status: PublicPlanPricesStatus; prices: PublicPlanPrices }

// One store for the page: every price consumer shares one catalogue request.
let snapshot: Snapshot = { status: "loading", prices: {} }
let inflight: Promise<void> | null = null
const listeners = new Set<() => void>()

function emit() {
  for (const listener of listeners) {
    listener()
  }
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

function getSnapshot(): Snapshot {
  return snapshot
}

/** Starts a request unless prices are ready or a request is already running. */
function loadPublicPlanPrices(): void {
  if (snapshot.status === "ready" || inflight != null) {
    return
  }
  snapshot = { status: "loading", prices: {} }
  emit()
  inflight = fetchPublicCustomUnitRates()
    .then((rates) => {
      snapshot = { status: "ready", prices: toPublicPlanPrices(rates.plans) }
    })
    .catch(() => {
      snapshot = { status: "failed", prices: {} }
    })
    .finally(() => {
      inflight = null
      emit()
    })
}

/** Plan prices from the published catalogue, shared across the page. */
export function usePublicPlanPrices(): PublicPlanPricesState {
  const state = useSyncExternalStore(subscribe, getSnapshot, getSnapshot)

  useEffect(() => {
    loadPublicPlanPrices()
  }, [])

  return { ...state, retry: loadPublicPlanPrices }
}
