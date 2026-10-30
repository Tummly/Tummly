import { useEffect, useState } from "react"

import { fetchPublicTrustIndicators } from "@/api/publicTrustIndicatorsApi"
import { trustClaimsVisible } from "@/content/marketing/publicTrustClaims"
import {
  normalizePublicTrustIndicators,
  type PublicTrustIndicator,
} from "@/lib/marketing/publicTrustIndicators"

export type PublicTrustIndicatorsState = {
  state: "loading" | "ready" | "error"
  items: PublicTrustIndicator[]
}

const CACHE_TTL_MS = 60_000

const LOADING: PublicTrustIndicatorsState = { state: "loading", items: [] }
const ERROR: PublicTrustIndicatorsState = { state: "error", items: [] }
/** Shown while the release gate is closed: no request, nothing to render. */
const GATED: PublicTrustIndicatorsState = { state: "ready", items: [] }

type CacheEntry = { at: number; request: Promise<PublicTrustIndicatorsState> }

/** Per-tab module state: one shared request, kept for 60 s. Errors are never kept. */
let cache: CacheEntry | null = null

function requestPublicTrustIndicators(): Promise<PublicTrustIndicatorsState> {
  return fetchPublicTrustIndicators().then(
    (raw) => {
      try {
        return { state: "ready", items: normalizePublicTrustIndicators(raw) }
      } catch {
        return ERROR
      }
    },
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
): Promise<PublicTrustIndicatorsState> {
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

/**
 * Emits the state to `onState`: `loading` first, then `ready` or `error`.
 * While the release gate is closed it emits `ready` with no items and makes no
 * request. After unsubscribe, nothing more is emitted. Returns the unsubscribe.
 */
export function subscribePublicTrustIndicators(
  onState: (state: PublicTrustIndicatorsState) => void,
): () => void {
  if (!trustClaimsVisible()) {
    onState(GATED)
    return () => undefined
  }

  let active = true
  onState(LOADING)
  void loadPublicTrustIndicators().then((next) => {
    if (active) onState(next)
  })
  return () => {
    active = false
  }
}

export function usePublicTrustIndicators(): PublicTrustIndicatorsState {
  const [state, setState] = useState<PublicTrustIndicatorsState>(LOADING)

  useEffect(() => subscribePublicTrustIndicators(setState), [])

  return state
}
