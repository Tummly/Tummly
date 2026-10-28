import { describe, expect, it, vi } from "vitest"

import {
  createDebouncedCall,
  type TimerScheduler,
} from "@/lib/guestPreferences/debouncedCall"

type FakeTimer = { run: () => void; delayMs: number; cancelled: boolean }

/** Timers that only fire when the test calls `advance`. */
function createFakeScheduler() {
  const timers: FakeTimer[] = []
  const scheduler: TimerScheduler = {
    schedule: (run, delayMs) => {
      const timer: FakeTimer = { run, delayMs, cancelled: false }
      timers.push(timer)
      return timer
    },
    cancel: (handle) => {
      ;(handle as FakeTimer).cancelled = true
    },
  }
  return {
    scheduler,
    /** Fires every live timer whose delay has been reached. */
    advance(ms: number) {
      for (const timer of timers) {
        if (!timer.cancelled && timer.delayMs <= ms) {
          timer.cancelled = true
          timer.run()
        }
      }
    },
    liveCount: () => timers.filter((timer) => !timer.cancelled).length,
  }
}

describe("createDebouncedCall", () => {
  it("runs once with the latest arguments after the delay", () => {
    const fake = createFakeScheduler()
    const run = vi.fn<(query: string) => void>()
    const debounced = createDebouncedCall(run, 300, fake.scheduler)

    debounced.call("K")
    debounced.call("KF")
    debounced.call("KFC")

    expect(run).not.toHaveBeenCalled()
    fake.advance(300)
    expect(run).toHaveBeenCalledTimes(1)
    expect(run).toHaveBeenCalledWith("KFC")
  })

  it("cancel drops the pending run", () => {
    const fake = createFakeScheduler()
    const run = vi.fn<(query: string) => void>()
    const debounced = createDebouncedCall(run, 300, fake.scheduler)

    debounced.call("KFC")
    debounced.cancel()
    fake.advance(300)

    expect(run).not.toHaveBeenCalled()
    expect(fake.liveCount()).toBe(0)
  })

  it("schedules a fresh run for a call made after the previous one ran", () => {
    const fake = createFakeScheduler()
    const run = vi.fn<(query: string) => void>()
    const debounced = createDebouncedCall(run, 300, fake.scheduler)

    debounced.call("K")
    fake.advance(300)
    debounced.call("KFC")
    fake.advance(300)

    expect(run.mock.calls).toEqual([["K"], ["KFC"]])
  })

  it("cancel is a no-op when nothing is pending", () => {
    const fake = createFakeScheduler()
    const run = vi.fn<(query: string) => void>()
    const debounced = createDebouncedCall(run, 300, fake.scheduler)

    expect(() => debounced.cancel()).not.toThrow()
    fake.advance(300)
    expect(run).not.toHaveBeenCalled()
  })
})
