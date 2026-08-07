/**
 * Fixed Messaging usage fixtures (Figma 3462:62679).
 * Shared source for overview + Channel step (ticket 24) — no live balance API.
 */

export const MESSAGING_USAGE_COPY = {
  title: "Messaging usage",
  subtitle:
    "Review the email allowance and SMS credits available to this operator account.",
  emailTitle: "Email allowance",
  smsTitle: "SMS credits",
  planTitle: "Current plan",
  viewUsage: "View messaging usage",
  buySmsCredits: "Buy SMS credits",
} as const

/** Raw Figma sample figures — Channel step must reuse these exact numbers. */
export const MESSAGING_USAGE_FIXTURE = {
  email: {
    used: 3240,
    allowance: 10000,
    remaining: 6760,
    refreshLabel: "15 August",
  },
  sms: {
    total: 420,
    reserved: 120,
    available: 300,
  },
  plan: {
    name: "Growth",
    locationCount: 3,
    billingLine: "Billed monthly · Next refresh 15 August",
  },
} as const

export type MessagingUsageFixture = typeof MESSAGING_USAGE_FIXTURE

export type OperatorCampaignsMessagingUsageViewModel = {
  title: string
  subtitle: string
  email: {
    title: string
    used: number
    allowance: number
    remaining: number
    usageLine: string
    detailLine: string
    meterMaxLabel: string
    /** 0–1 fill for the usage meter (used / allowance). */
    fillRatio: number
  }
  sms: {
    title: string
    total: number
    reserved: number
    available: number
    usageLine: string
    detailLine: string
    meterMaxLabel: string
    /** 0–1 fill for the usage meter (available / total). */
    fillRatio: number
  }
  plan: {
    title: string
    name: string
    locationCount: number
    planLine: string
    billingLine: string
  }
  viewUsageLabel: string
  buySmsCreditsLabel: string
}

function formatCount(value: number): string {
  return value.toLocaleString("en-GB")
}

export function messagingUsageViewModelFromFixture(
  fixture: MessagingUsageFixture = MESSAGING_USAGE_FIXTURE
): OperatorCampaignsMessagingUsageViewModel {
  const { email, sms, plan } = fixture

  return {
    title: MESSAGING_USAGE_COPY.title,
    subtitle: MESSAGING_USAGE_COPY.subtitle,
    email: {
      title: MESSAGING_USAGE_COPY.emailTitle,
      used: email.used,
      allowance: email.allowance,
      remaining: email.remaining,
      usageLine: `${formatCount(email.used)} of ${formatCount(email.allowance)} used`,
      detailLine: `${formatCount(email.remaining)} remaining · Refreshes ${email.refreshLabel}`,
      meterMaxLabel: formatCount(email.allowance),
      fillRatio: email.used / email.allowance,
    },
    sms: {
      title: MESSAGING_USAGE_COPY.smsTitle,
      total: sms.total,
      reserved: sms.reserved,
      available: sms.available,
      usageLine: `${formatCount(sms.total)} total`,
      detailLine: `${formatCount(sms.reserved)} reserved · ${formatCount(sms.available)} available`,
      meterMaxLabel: formatCount(sms.available),
      fillRatio: sms.available / sms.total,
    },
    plan: {
      title: MESSAGING_USAGE_COPY.planTitle,
      name: plan.name,
      locationCount: plan.locationCount,
      planLine: `${plan.name} · ${plan.locationCount} locations`,
      billingLine: plan.billingLine,
    },
    viewUsageLabel: MESSAGING_USAGE_COPY.viewUsage,
    buySmsCreditsLabel: MESSAGING_USAGE_COPY.buySmsCredits,
  }
}
