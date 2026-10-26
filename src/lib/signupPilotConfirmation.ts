export type PilotSetupDates = {
  subscriptionPlan: string
  pilotStartedAtUtc?: string | null
  pilotEndsAtUtc?: string | null
}

// Same day format as the Manage Plan Pilot label (UK, long month, UTC date).
function formatPilotDate(value: string | null | undefined): string | null {
  if (!value) return null
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return null
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "UTC",
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(date)
}

/** Setup confirmation line for a Pilot account. Null when there is nothing to state. */
export function buildPilotSetupConfirmation(
  plan: PilotSetupDates | null | undefined
): string | null {
  if (!plan || plan.subscriptionPlan !== "Pilot") return null
  const start = formatPilotDate(plan.pilotStartedAtUtc)
  const end = formatPilotDate(plan.pilotEndsAtUtc)
  if (!start || !end) return null
  return `Your 30-day Pilot started on ${start} and ends on ${end}.`
}
