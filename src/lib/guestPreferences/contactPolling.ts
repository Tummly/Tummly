export const CONTACT_POLL_INTERVAL_MS = 5000

/** The timer calls `startContactPolling` uses. Tests pass a manual one. */
export type PollScheduler = {
  setInterval: (callback: () => void, ms: number) => unknown
  clearInterval: (handle: unknown) => void
}

export const windowPollScheduler: PollScheduler = {
  setInterval: (callback, ms) => window.setInterval(callback, ms),
  clearInterval: (handle) => window.clearInterval(handle as number),
}

export type ContactPollOptions = {
  /** Resolves true once the new contact is on the overview. */
  check: () => Promise<boolean>
  onFound: () => void
  /** Called for each failed check. Polling continues unless the caller stops it. */
  onError: (error: unknown) => void
  intervalMs?: number
  scheduler?: PollScheduler
}

/**
 * Runs `check` on a fixed interval until it resolves true, or until the
 * returned stop function runs. A tick is skipped while a check is in flight,
 * and a result that arrives after stop is ignored.
 */
export function startContactPolling({
  check,
  onFound,
  onError,
  intervalMs = CONTACT_POLL_INTERVAL_MS,
  scheduler = windowPollScheduler,
}: ContactPollOptions): () => void {
  let stopped = false
  let inFlight = false

  const handle = scheduler.setInterval(() => {
    if (inFlight) {
      return
    }
    inFlight = true
    check()
      .then(
        (found) => {
          if (stopped || !found) {
            return
          }
          stop()
          onFound()
        },
        (error: unknown) => {
          if (!stopped) {
            onError(error)
          }
        }
      )
      .finally(() => {
        inFlight = false
      })
  }, intervalMs)

  function stop(): void {
    if (stopped) {
      return
    }
    stopped = true
    scheduler.clearInterval(handle)
  }

  return stop
}
