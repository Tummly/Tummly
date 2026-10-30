import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

import { fetchPublicTrustIndicators } from "@/api/publicTrustIndicatorsApi"
import { trustClaimsVisible } from "@/content/marketing/publicTrustClaims"
import {
  normalizePublicTrustIndicators,
  type PublicTrustIndicator,
} from "@/lib/marketing/publicTrustIndicators"

import {
  loadPublicTrustIndicators,
  resetPublicTrustIndicatorsCache,
  subscribePublicTrustIndicators,
  type PublicTrustIndicatorsState,
} from "./usePublicTrustIndicators"

vi.mock("@/api/publicTrustIndicatorsApi", () => ({
  fetchPublicTrustIndicators: vi.fn(),
}))

vi.mock("@/content/marketing/publicTrustClaims", () => ({
  trustClaimsVisible: vi.fn(() => true),
}))

vi.mock("@/lib/marketing/publicTrustIndicators", async (importOriginal) => {
  const actual = await importOriginal<
    typeof import("@/lib/marketing/publicTrustIndicators")
  >()
  return {
    ...actual,
    normalizePublicTrustIndicators: vi.fn(actual.normalizePublicTrustIndicators),
  }
})

const fetchMock = vi.mocked(fetchPublicTrustIndicators)
const gateMock = vi.mocked(trustClaimsVisible)
const normalizeMock = vi.mocked(normalizePublicTrustIndicators)

const LOADING: PublicTrustIndicatorsState = { state: "loading", items: [] }

const payload = [
  {
    itemNumber: 1,
    title: "ICO Data Protection Registration",
    status: "Verified",
    statusLabel: "Registered",
    iconKey: "ico-data",
    imageUrl: null,
    altText: "",
    tooltip: "",
    cta: null,
  },
]

/** Lets the chained promise callbacks run under fake timers. */
async function flush() {
  for (let i = 0; i < 10; i++) await Promise.resolve()
}

function subscribe() {
  const states: PublicTrustIndicatorsState[] = []
  const unsubscribe = subscribePublicTrustIndicators((state) => states.push(state))
  return { states, unsubscribe }
}

beforeEach(() => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(0))
  resetPublicTrustIndicatorsCache()
  fetchMock.mockReset()
  fetchMock.mockResolvedValue(payload)
  gateMock.mockReturnValue(true)
  normalizeMock.mockClear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe("loadPublicTrustIndicators cache", () => {
  it("shares one fetch between calls inside 60 seconds", async () => {
    const first = await loadPublicTrustIndicators()
    vi.setSystemTime(59_999)
    const second = await loadPublicTrustIndicators()
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(first.state).toBe("ready")
    expect(second.items).toHaveLength(1)
  })

  it("shares one in-flight request between concurrent calls", async () => {
    await Promise.all([loadPublicTrustIndicators(), loadPublicTrustIndicators()])
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it("refetches after 61 seconds", async () => {
    await loadPublicTrustIndicators()
    vi.setSystemTime(61_000)
    await loadPublicTrustIndicators()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it("returns error with no items when the fetch rejects, and the next call retries", async () => {
    fetchMock.mockRejectedValueOnce(new Error("network"))
    const failed = await loadPublicTrustIndicators()
    expect(failed).toEqual({ state: "error", items: [] })

    const retried = await loadPublicTrustIndicators()
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(retried.state).toBe("ready")
    expect(retried.items.map((item: PublicTrustIndicator) => item.itemNumber)).toEqual([1])
  })

  it("normalizes the payload before returning it", async () => {
    fetchMock.mockResolvedValueOnce([{ ...payload[0], status: "Certified" }])
    const result = await loadPublicTrustIndicators()
    expect(result).toEqual({ state: "ready", items: [] })
  })

  it("turns a throwing normalizer into error, not a stuck loading state", async () => {
    normalizeMock.mockImplementationOnce(() => {
      throw new Error("bad payload")
    })
    const result = await loadPublicTrustIndicators()
    expect(result).toEqual({ state: "error", items: [] })
  })
})

describe("subscribePublicTrustIndicators", () => {
  it("emits loading, then ready", async () => {
    const { states } = subscribe()
    expect(states).toEqual([LOADING])
    await flush()
    expect(states).toHaveLength(2)
    expect(states[1].state).toBe("ready")
    expect(states[1].items.map((item) => item.itemNumber)).toEqual([1])
  })

  it("emits loading, then error; a second subscribe retries and reaches ready", async () => {
    fetchMock.mockRejectedValueOnce(new Error("network"))
    const first = subscribe()
    await flush()
    expect(first.states.map((s) => s.state)).toEqual(["loading", "error"])

    const second = subscribe()
    await flush()
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(second.states.map((s) => s.state)).toEqual(["loading", "ready"])
  })

  it("emits nothing after unsubscribe, even when the fetch resolves later", async () => {
    let resolveFetch: (value: unknown) => void = () => undefined
    fetchMock.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveFetch = resolve
      }),
    )
    const { states, unsubscribe } = subscribe()
    unsubscribe()
    resolveFetch(payload)
    await flush()
    expect(states).toEqual([LOADING])
  })

  it("makes no request and emits ready with no items while the gate is closed", async () => {
    gateMock.mockReturnValue(false)
    const { states } = subscribe()
    await flush()
    expect(states).toEqual([{ state: "ready", items: [] }])
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
