const MS_PER_DAY = 24 * 60 * 60 * 1000

/** Urgency of the Activation period badge by days remaining. */
export type ActivationPeriodBadgeTone = "default" | "warning" | "urgent"

export interface ActivationPeriodBadgeCopy {
  /** Customer-facing product label, e.g. "Advanced trial". */
  title: string
  /** Countdown fragment, e.g. "14 days left". */
  remaining: string
  /** Visual urgency: >15 default, ≤15 warning, ≤5 urgent. */
  tone: ActivationPeriodBadgeTone
}

/**
 * Days remaining in the Activation period for the Advanced trial badge.
 * Returns null when expiry is missing or the period has ended (hide badge).
 */
export function computeActivationDaysRemaining(
  activationExpiresAt: string | null | undefined,
  now: Date = new Date()
): number | null {
  if (!activationExpiresAt) {
    return null
  }

  const expiresAtMs = Date.parse(activationExpiresAt)

  if (Number.isNaN(expiresAtMs)) {
    return null
  }

  const remainingMs = expiresAtMs - now.getTime()

  if (remainingMs <= 0) {
    return null
  }

  return Math.ceil(remainingMs / MS_PER_DAY)
}

/**
 * Badge tone from days remaining: urgent ≤5, warning ≤15, else default.
 */
export function activationPeriodBadgeTone(
  daysRemaining: number
): ActivationPeriodBadgeTone {
  if (daysRemaining <= 5) {
    return "urgent"
  }

  if (daysRemaining <= 15) {
    return "warning"
  }

  return "default"
}

/**
 * Split Advanced trial badge copy (Activation period remaining).
 * Null means omit the badge.
 */
export function formatActivationPeriodBadge(
  daysRemaining: number | null
): ActivationPeriodBadgeCopy | null {
  if (daysRemaining == null || daysRemaining <= 0) {
    return null
  }

  const dayWord = daysRemaining === 1 ? "day" : "days"
  return {
    title: "Advanced trial",
    remaining: `${daysRemaining} ${dayWord} left`,
    tone: activationPeriodBadgeTone(daysRemaining),
  }
}
