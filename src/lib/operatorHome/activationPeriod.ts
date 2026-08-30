const MS_PER_DAY = 24 * 60 * 60 * 1000

/** Urgency of the Activation period badge by days remaining. */
export type ActivationPeriodBadgeTone = "default" | "warning" | "urgent"

export interface ActivationPeriodBadgeCopy {
  /** Countdown fragment, e.g. "14 days left". */
  remaining: string
  /** Calendar end date in en-GB form, e.g. "13 Aug 2026". */
  endsOn: string
  /** Visual urgency: >15 default, ≤15 warning, ≤5 urgent. */
  tone: ActivationPeriodBadgeTone
}

/** Home hero badge — copy plus optional Choose a plan link. */
export interface ActivationPeriodBadgePresentation extends ActivationPeriodBadgeCopy {
  /** Same-shell manage-plan href; null hides the underlined CTA. */
  choosePlanHref: string | null
}

/**
 * Days remaining in the Activation period for the Activation period badge.
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

/** Format Activation expiry for the badge end-date fragment (en-GB). */
export function formatActivationPeriodEndsOn(
  activationExpiresAt: string
): string | null {
  const expiresAtMs = Date.parse(activationExpiresAt)
  if (Number.isNaN(expiresAtMs)) {
    return null
  }

  return new Date(expiresAtMs).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })
}

/**
 * Activation period badge copy (remaining days + end date).
 * Null means omit the badge.
 */
export function formatActivationPeriodBadge(
  daysRemaining: number | null,
  activationExpiresAt: string | null | undefined
): ActivationPeriodBadgeCopy | null {
  if (
    daysRemaining == null ||
    daysRemaining <= 0 ||
    activationExpiresAt == null ||
    activationExpiresAt === ""
  ) {
    return null
  }

  const endsOn = formatActivationPeriodEndsOn(activationExpiresAt)
  if (endsOn == null) {
    return null
  }

  const dayWord = daysRemaining === 1 ? "day" : "days"
  return {
    remaining: `${daysRemaining} ${dayWord} left`,
    endsOn,
    tone: activationPeriodBadgeTone(daysRemaining),
  }
}

/**
 * Activation period badge for Home — Pilot only while the period has time.
 * Returns null when the live plan is paid or the period has ended.
 */
export function resolveActivationPeriodBadgePresentation(options: {
  subscriptionPlan: string
  activationExpiresAt: string | null | undefined
  choosePlanHref: string | null
  now?: Date
}): ActivationPeriodBadgePresentation | null {
  if (options.subscriptionPlan !== "Pilot") {
    return null
  }

  const daysRemaining = computeActivationDaysRemaining(
    options.activationExpiresAt,
    options.now
  )
  const copy = formatActivationPeriodBadge(
    daysRemaining,
    options.activationExpiresAt
  )

  if (copy == null) {
    return null
  }

  return {
    ...copy,
    choosePlanHref: options.choosePlanHref,
  }
}
