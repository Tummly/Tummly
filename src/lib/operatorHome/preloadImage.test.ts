import { afterEach, describe, expect, it, vi } from "vitest"

import { preloadImage } from "./preloadImage"

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe("preloadImage", () => {
  it("resolves ready when the image loads", async () => {
    vi.stubGlobal(
      "Image",
      class {
        onload: (() => void) | null = null
        onerror: (() => void) | null = null
        decoding = "async"
        set src(_value: string) {
          queueMicrotask(() => this.onload?.())
        }
        decode() {
          return Promise.resolve()
        }
      }
    )

    await expect(preloadImage("/hero.webp", 1000)).resolves.toBe("ready")
  })

  it("resolves timeout when load is slow", async () => {
    vi.useFakeTimers()
    vi.stubGlobal(
      "Image",
      class {
        onload: (() => void) | null = null
        onerror: (() => void) | null = null
        set src(_value: string) {
          /* never load */
        }
        decode() {
          return new Promise(() => {})
        }
      }
    )

    const pending = preloadImage("/hero.webp", 50)
    await vi.advanceTimersByTimeAsync(50)
    await expect(pending).resolves.toBe("timeout")
    vi.useRealTimers()
  })
})
