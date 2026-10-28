/** Timer access, injectable so tests can drive time without fake timers. */
export type TimerScheduler = {
  schedule: (run: () => void, delayMs: number) => unknown
  cancel: (handle: unknown) => void
}

export const browserTimerScheduler: TimerScheduler = {
  schedule: (run, delayMs) => setTimeout(run, delayMs),
  cancel: (handle) => clearTimeout(handle as ReturnType<typeof setTimeout>),
}

export type DebouncedCall<Args extends unknown[]> = {
  call: (...args: Args) => void
  /** Drops the pending call, if any. */
  cancel: () => void
}

/**
 * Runs `run` once, `delayMs` after the last `call`. Each call replaces the
 * pending one, so only the latest arguments run.
 */
export function createDebouncedCall<Args extends unknown[]>(
  run: (...args: Args) => void,
  delayMs: number,
  scheduler: TimerScheduler = browserTimerScheduler
): DebouncedCall<Args> {
  let pending = false
  let handle: unknown = null

  function cancel() {
    if (pending) {
      pending = false
      scheduler.cancel(handle)
    }
  }

  return {
    call(...args: Args) {
      cancel()
      pending = true
      handle = scheduler.schedule(() => {
        pending = false
        run(...args)
      }, delayMs)
    },
    cancel,
  }
}
