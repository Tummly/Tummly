import { useEffect, useState } from "react"

import { fetchPublicTrustIndicators } from "@/api/publicTrustIndicatorsApi"
import {
  normalizePublicTrustIndicators,
  type PublicTrustIndicator,
} from "@/lib/marketing/publicTrustIndicators"

export type PublicTrustIndicatorsResult = {
  state: "loading" | "ready" | "error"
  items: PublicTrustIndicator[]
}

const CACHE_TTL_MS = 60_000

const LOADING: PublicTrustIndicatorsResult = { state: "loading", items: [] }
const ERROR: PublicTrustIndicatorsResult = { state: "error", items: [] }

type CacheEntry = { at: number; request: Promise<PublicTrustIndicatorsResult> }

/** Per-tab module state: one shared request, kept for 60 s. Errors are never kept. */
let cache: CacheEntry | null = null

function requestPublicTrustIndicators(): Promise<PublicTrustIndicatorsResult> {
  return fetchPublicTrustIndicators().then(
    (raw) => ({ state: "ready", items: normalizePublicTrustIndicators(raw) }),
    () => ERROR,
  )
}

/**
 * Returns the shared trust indicators. Concurrent and repeat calls within 60 s
 * reuse one request; a failed request is dropped so the next call retries.
 * Never rejects.
 */
export function loadPublicTrustIndicators(
  now: number = Date.now(),
): Promise<PublicTrustIndicatorsResult> {
  if (cache !== null && now - cache.at < CACHE_TTL_MS) {
    return cache.request
  }

  const entry: CacheEntry = {
    at: now,
    request: requestPublicTrustIndicators().then((result) => {
      if (result.state === "error" && cache === entry) cache = null
      return result
    }),
  }
  cache = entry
  return entry.request
}

/** Clears the shared cache. Tests only. */
export function resetPublicTrustIndicatorsCache(): void {
  cache = null
}

export function usePublicTrustIndicators(): PublicTrustIndicatorsResult {
  const [result, setResult] = useState<PublicTrustIndicatorsResult>(LOADING)

  useEffect(() => {
    let active = true
    void loadPublicTrustIndicators().then((next) => {
      if (active) setResult(next)
    })
    return () => {
      active = false
    }
  }, [])

  return result
}
