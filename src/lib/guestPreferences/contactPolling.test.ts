import { describe, expect, it } from "vitest"

import {
  CONTACT_POLL_INTERVAL_MS,
  startContactPolling,
  type PollScheduler,
} from "./contactPolling"

/** A scheduler the test drives by hand: `tick()` runs the registered callback. */
function manualScheduler() {
  let callback: (() => void) | null = null
  let intervalMs: number | null = null
  let cleared = 0
  const scheduler: PollScheduler = {
    setInterval: (fn, ms) => {
      callback = fn
      intervalMs = ms
      return "handle"
    },
    clearInterval: () => {
      cleared += 1
    },
  }
  return {
    scheduler,
    tick: () => callback?.(),
    intervalMs: () => intervalMs,
    clearedCount: () => cleared,
  }
}

/** Lets pending promise callbacks run. */
function flush(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0))
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

describe("startContactPolling", () => {
  it("polls every 5 seconds by default and does not check before the first tick", () => {
    const timer = manualScheduler()
    let checks = 0

    startContactPolling({
      check: async () => {
        checks += 1
        return false
      },
      onFound: () => undefined,
      onError: () => undefined,
      scheduler: timer.scheduler,
    })

    expect(timer.intervalMs()).toBe(CONTACT_POLL_INTERVAL_MS)
    expect(CONTACT_POLL_INTERVAL_MS).toBe(5000)
    expect(checks).toBe(0)
  })

  it("keeps polling while the contact is not on the overview yet", async () => {
    const timer = manualScheduler()
    let checks = 0
    let found = 0

    startContactPolling({
      check: async () => {
        checks += 1
        return false
      },
      onFound: () => {
        found += 1
      },
      onError: () => undefined,
      scheduler: timer.scheduler,
    })
    timer.tick()
    await flush()
    timer.tick()
    await flush()

    expect(checks).toBe(2)
    expect(found).toBe(0)
    expect(timer.clearedCount()).toBe(0)
  })

  it("calls onFound once and stops when the contact appears", async () => {
    const timer = manualScheduler()
    let found = 0

    startContactPolling({
      check: async () => true,
      onFound: () => {
        found += 1
      },
      onError: () => undefined,
      scheduler: timer.scheduler,
    })
    timer.tick()
    await flush()

    expect(found).toBe(1)
    expect(timer.clearedCount()).toBe(1)
  })

  it("stop() ends polling and ignores a check still in flight", async () => {
    const timer = manualScheduler()
    const pending = deferred<boolean>()
    let found = 0

    const stop = startContactPolling({
      check: () => pending.promise,
      onFound: () => {
        found += 1
      },
      onError: () => undefined,
      scheduler: timer.scheduler,
    })
    timer.tick()
    stop()
    pending.resolve(true)
    await flush()

    expect(found).toBe(0)
    expect(timer.clearedCount()).toBe(1)
  })

  it("stop() is safe to call twice", () => {
    const timer = manualScheduler()
    const stop = startContactPolling({
      check: async () => false,
      onFound: () => undefined,
      onError: () => undefined,
      scheduler: timer.scheduler,
    })

    stop()
    stop()

    expect(timer.clearedCount()).toBe(1)
  })

  it("does not start a second check while one is still in flight", async () => {
    const timer = manualScheduler()
    const pending = deferred<boolean>()
    let checks = 0

    startContactPolling({
      check: () => {
        checks += 1
        return pending.promise
      },
      onFound: () => undefined,
      onError: () => undefined,
      scheduler: timer.scheduler,
    })
    timer.tick()
    timer.tick()

    expect(checks).toBe(1)

    pending.resolve(false)
    await flush()
    timer.tick()

    expect(checks).toBe(2)
  })

  it("reports a failed check and keeps polling", async () => {
    const timer = manualScheduler()
    const failure = new Error("offline")
    const errors: unknown[] = []
    let checks = 0

    startContactPolling({
      check: async () => {
        checks += 1
        throw failure
      },
      onFound: () => undefined,
      onError: (error) => {
        errors.push(error)
      },
      scheduler: timer.scheduler,
    })
    timer.tick()
    await flush()
    timer.tick()
    await flush()

    expect(errors).toEqual([failure, failure])
    expect(checks).toBe(2)
  })
})
