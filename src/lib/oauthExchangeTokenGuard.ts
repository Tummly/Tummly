/** In-flight / consumed one-time exchange tickets (survives Strict Mode remount). */
const claimedTokens = new Map<string, Promise<unknown>>()

/**
 * Runs `runner` once per token. Remounts reuse the same promise so the
 * one-time ticket is not exchanged twice.
 */
export function runOAuthExchangeOnce<T>(
  token: string,
  runner: () => Promise<T>
): Promise<T> {
  const key = token.trim()
  if (!key) {
    return Promise.reject(new Error("Missing OAuth exchange token."))
  }

  const existing = claimedTokens.get(key)
  if (existing) {
    return existing as Promise<T>
  }

  const promise = runner()
  claimedTokens.set(key, promise)
  // Drop the claim on failure so the same ticket can retry after a network error.
  // Keep the claim after success and while in flight (Strict Mode remount reuse).
  void promise.then(
    () => undefined,
    () => {
      if (claimedTokens.get(key) === promise) {
        claimedTokens.delete(key)
      }
    }
  )
  return promise
}

/** Test helper — clears claimed tokens between cases. */
export function resetOAuthExchangeTokenGuardForTests(): void {
  claimedTokens.clear()
}
