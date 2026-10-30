import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

import { fetchPublicTrustIndicators } from "@/api/publicTrustIndicatorsApi"

import {
  loadPublicTrustIndicators,
  resetPublicTrustIndicatorsCache,
} from "./usePublicTrustIndicators"

vi.mock("@/api/publicTrustIndicatorsApi", () => ({
  fetchPublicTrustIndicators: vi.fn(),
}))

const fetchMock = vi.mocked(fetchPublicTrustIndicators)

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

beforeEach(() => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date(0))
  resetPublicTrustIndicatorsCache()
  fetchMock.mockReset()
  fetchMock.mockResolvedValue(payload)
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
    expect(retried.items.map((item) => item.itemNumber)).toEqual([1])
  })

  it("normalizes the payload before returning it", async () => {
    fetchMock.mockResolvedValueOnce([{ ...payload[0], status: "Certified" }])
    const result = await loadPublicTrustIndicators()
    expect(result).toEqual({ state: "ready", items: [] })
  })
})
