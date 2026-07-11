import { toast } from "sonner"

/** Shared client helper mirroring backend EmailDispatch.DefaultWarning. */
export const EMAIL_DISPATCH_DEFAULT_WARNING =
  "Saved, but the notification email could not be sent."

export type EmailDispatchMeta = {
  emailDispatched?: boolean | null
  emailWarning?: string | null
}

export function parseEmailDispatchMeta(
  raw: Record<string, unknown>
): EmailDispatchMeta {
  if (
    !("emailDispatched" in raw)
    || raw.emailDispatched === null
    || raw.emailDispatched === undefined
  ) {
    return {}
  }

  return {
    emailDispatched: Boolean(raw.emailDispatched),
    emailWarning:
      typeof raw.emailWarning === "string" && raw.emailWarning.trim()
        ? raw.emailWarning.trim()
        : null,
  }
}

/**
 * Shows a warning toast when a mutation saved but the notification email failed.
 * Returns true when email succeeded or was not attempted.
 */
export function warnIfEmailDispatchFailed(
  meta: EmailDispatchMeta,
  fallbackWarning = EMAIL_DISPATCH_DEFAULT_WARNING
): boolean {
  if (meta.emailDispatched !== false) {
    return true
  }

  toast.warning(meta.emailWarning?.trim() || fallbackWarning)
  return false
}
