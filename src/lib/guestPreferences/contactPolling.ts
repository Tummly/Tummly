export const CONTACT_POLL_INTERVAL_MS = 5000

/** The add-contact link lifetime. Polling stops once it has passed. */
export const CONTACT_POLL_TIMEOUT_MS = 10 * 60 * 1000

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
  /** Polling stops after this long. The dialog then offers Resend. */
  timeoutMs?: number
  scheduler?: PollScheduler
  now?: () => number
}

/**
 * Runs `check` on a fixed interval until it resolves true, the timeout passes, or
 * the returned stop function runs. A tick is skipped while a check is in flight,
 * and a result that arrives after stop is ignored.
 */
export function startContactPolling({
  check,
  onFound,
  onError,
  intervalMs = CONTACT_POLL_INTERVAL_MS,
  timeoutMs = CONTACT_POLL_TIMEOUT_MS,
  scheduler = windowPollScheduler,
  now = Date.now,
}: ContactPollOptions): () => void {
  let stopped = false
  let inFlight = false
  const startedAt = now()

  const handle = scheduler.setInterval(() => {
    if (now() - startedAt >= timeoutMs) {
      stop()
      return
    }
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

/**
 * True when the overview shows the address the guest typed. Either the contact count
 * has grown past the baseline, or a contact has the typed address's masked form. The
 * second case covers an address already on the guest's profile, where the count never grows.
 */
export function overviewConfirmsContact({
  contacts,
  baselineCount,
  maskedTyped,
}: {
  contacts: ReadonlyArray<{ masked: string }>
  baselineCount: number
  /** The typed address masked the way the server masks it. */
  maskedTyped: string
}): boolean {
  return (
    contacts.length > baselineCount ||
    contacts.some((contact) => contact.masked === maskedTyped)
  )
}
