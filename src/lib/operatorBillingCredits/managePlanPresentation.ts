import type { PlanSubscriptionSnapshot } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"

export const MANAGE_PLAN_IDS = ["Pilot", "Starter", "Growth", "Group"] as const

export type ManagePlanId = (typeof MANAGE_PLAN_IDS)[number]

export type BillingCadence = "monthly" | "annual"

export type PlanChangeKind =
  | "convert"
  | "upgrade"
  | "downgrade"
  | "cadence-only"
  | "plan-and-cadence"

export type PlanCardCta =
  | {
      kind: "current"
      label: "Current plan"
      disabled: true
    }
  | {
      kind: "disabled"
      label: string
      disabled: true
    }
  | {
      kind: "action"
      label: string
      disabled: false
      changeKind: PlanChangeKind
    }

export type ManagePlanCardViewModel = {
  id: ManagePlanId
  description: string
  priceHeadline: string
  annualSaveLabel: string | null
  emailCreditsLabel: string
  smsCreditsLabel: string
  aiCreditsLabel: string
  cta: PlanCardCta
}

type PlanCatalogEntry = {
  description: string
  monthlyPriceNet: string
  annualPriceNet: string
  annualSaveLabel: string
  emailCreditsLabel: string
  smsCreditsLabel: string
  aiCreditsLabel: string
}

const PLAN_RANK: Record<ManagePlanId, number> = {
  Pilot: 0,
  Starter: 1,
  Growth: 2,
  Group: 3,
}

export const MANAGE_PLAN_CATALOG: Record<ManagePlanId, PlanCatalogEntry> = {
  Pilot: {
    description:
      "Try Guest Loop in one real Location before choosing a paid plan.",
    monthlyPriceNet: "£0",
    annualPriceNet: "£0",
    annualSaveLabel: "",
    emailCreditsLabel: "500 once",
    smsCreditsLabel: "20 once",
    aiCreditsLabel: "20 once",
  },
  Starter: {
    description:
      "For one Location that wants to hear from guests, build a guest list and bring people back.",
    monthlyPriceNet: "£39",
    annualPriceNet: "£398",
    annualSaveLabel: "Save £70 per year",
    emailCreditsLabel: "2,500 / month",
    smsCreditsLabel: "100 / month",
    aiCreditsLabel: "100 / month",
  },
  Growth: {
    description:
      "For operators with multiple Locations who need higher message volume and more team seats.",
    monthlyPriceNet: "£99",
    annualPriceNet: "£1,010",
    annualSaveLabel: "Save £178 per year",
    emailCreditsLabel: "10,000 / month",
    smsCreditsLabel: "350 / month",
    aiCreditsLabel: "500 / month",
  },
  Group: {
    description:
      "For groups with five or more Locations, higher allowances, and optional extra Locations.",
    monthlyPriceNet: "£199",
    annualPriceNet: "£2,030",
    annualSaveLabel: "Save £358 per year",
    emailCreditsLabel: "25,000 / month",
    smsCreditsLabel: "700 / month",
    aiCreditsLabel: "1,500 / month",
  },
}

export const MANAGE_PLAN_COPY = {
  currentPlanHeading: "Current plan",
  cadenceMonthly: "Monthly",
  cadenceAnnual: "Annual",
  vatNotice:
    "Prices shown exclude VAT. VAT is added at checkout when applicable.",
  choosePlan: (plan: ManagePlanId) => `Choose ${plan}`,
  upgradeTo: (plan: ManagePlanId) => `Upgrade to ${plan}`,
  downgradeTo: (plan: ManagePlanId) => `Downgrade to ${plan}`,
  switchToAnnual: "Switch to annual",
  switchToMonthly: "Switch to monthly",
  confirmConvertTitle: (plan: ManagePlanId) => `Choose ${plan}`,
  confirmUpgradeTitle: (plan: ManagePlanId) => `Upgrade to ${plan}`,
  confirmDowngradeTitle: (plan: ManagePlanId) => `Downgrade to ${plan}`,
  confirmCadenceTitle: (cadence: BillingCadence) =>
    cadence === "annual" ? "Switch to annual billing" : "Switch to monthly billing",
  confirmPayBody:
    "You will confirm this change on Tummly, then pay on Revolut. Your plan updates after payment succeeds.",
  confirmScheduleBody: (renewalDateLabel: string | null) =>
    renewalDateLabel == null
      ? "This change takes effect on your renewal date."
      : `This change takes effect on ${renewalDateLabel.replace(/^Renews /, "")}.`,
  confirmPrimaryPay: "Confirm and pay",
  confirmPrimarySchedule: "Confirm change",
} as const

function normalizePlanId(plan: string): ManagePlanId {
  if ((MANAGE_PLAN_IDS as readonly string[]).includes(plan)) {
    return plan as ManagePlanId
  }
  return "Pilot"
}

export function liveCadenceFromSnapshot(
  plan: PlanSubscriptionSnapshot
): BillingCadence | null {
  if (plan.isPilot || plan.billingCycle == null) {
    return null
  }
  return plan.billingCycle.toLowerCase() === "annual" ? "annual" : "monthly"
}

export function defaultPreviewCadence(
  plan: PlanSubscriptionSnapshot
): BillingCadence {
  return liveCadenceFromSnapshot(plan) ?? "monthly"
}

export function resolvePlanChangeKind(options: {
  currentPlanId: ManagePlanId
  targetPlanId: ManagePlanId
  liveCadence: BillingCadence | null
  previewCadence: BillingCadence
}): PlanChangeKind {
  const { currentPlanId, targetPlanId, liveCadence, previewCadence } = options

  if (currentPlanId === targetPlanId) {
    return "cadence-only"
  }

  if (currentPlanId === "Pilot") {
    return "convert"
  }

  if (liveCadence != null && previewCadence !== liveCadence) {
    return "plan-and-cadence"
  }

  const currentRank = PLAN_RANK[currentPlanId]
  const targetRank = PLAN_RANK[targetPlanId]

  if (targetRank > currentRank) {
    return "upgrade"
  }

  return "downgrade"
}

export function resolvePlanCardCta(options: {
  cardPlanId: ManagePlanId
  currentPlanId: ManagePlanId
  isPilot: boolean
  liveCadence: BillingCadence | null
  previewCadence: BillingCadence
}): PlanCardCta {
  const { cardPlanId, currentPlanId, isPilot, liveCadence, previewCadence } =
    options

  if (cardPlanId === "Pilot" && !isPilot) {
    return {
      kind: "disabled",
      label: MANAGE_PLAN_COPY.choosePlan("Pilot"),
      disabled: true,
    }
  }

  if (cardPlanId === currentPlanId) {
    if (
      !isPilot
      && liveCadence != null
      && previewCadence !== liveCadence
    ) {
      return {
        kind: "action",
        label:
          previewCadence === "annual"
            ? MANAGE_PLAN_COPY.switchToAnnual
            : MANAGE_PLAN_COPY.switchToMonthly,
        disabled: false,
        changeKind: "cadence-only",
      }
    }

    return {
      kind: "current",
      label: "Current plan",
      disabled: true,
    }
  }

  const changeKind = resolvePlanChangeKind({
    currentPlanId,
    targetPlanId: cardPlanId,
    liveCadence,
    previewCadence,
  })

  if (changeKind === "convert") {
    return {
      kind: "action",
      label: MANAGE_PLAN_COPY.choosePlan(cardPlanId),
      disabled: false,
      changeKind,
    }
  }

  if (changeKind === "upgrade" || changeKind === "plan-and-cadence") {
    const isUpgrade =
      changeKind === "upgrade"
      || PLAN_RANK[cardPlanId] > PLAN_RANK[currentPlanId]
    return {
      kind: "action",
      label: isUpgrade
        ? MANAGE_PLAN_COPY.upgradeTo(cardPlanId)
        : MANAGE_PLAN_COPY.downgradeTo(cardPlanId),
      disabled: false,
      changeKind,
    }
  }

  return {
    kind: "action",
    label: MANAGE_PLAN_COPY.downgradeTo(cardPlanId),
    disabled: false,
    changeKind,
  }
}

function formatPriceHeadline(
  planId: ManagePlanId,
  cadence: BillingCadence
): string {
  const entry = MANAGE_PLAN_CATALOG[planId]
  if (planId === "Pilot") {
    return `${entry.monthlyPriceNet} / for 30 days`
  }
  if (cadence === "annual") {
    return `${entry.annualPriceNet} / year + VAT`
  }
  return `${entry.monthlyPriceNet} / month + VAT`
}

export function buildManagePlanCardViewModels(options: {
  plan: PlanSubscriptionSnapshot
  previewCadence: BillingCadence
}): ManagePlanCardViewModel[] {
  const currentPlanId = normalizePlanId(options.plan.subscriptionPlan)
  const liveCadence = liveCadenceFromSnapshot(options.plan)

  return MANAGE_PLAN_IDS.map((id) => {
    const entry = MANAGE_PLAN_CATALOG[id]
    const annualSaveLabel =
      id === "Pilot" || options.previewCadence === "monthly"
        ? null
        : entry.annualSaveLabel

    return {
      id,
      description: entry.description,
      priceHeadline: formatPriceHeadline(id, options.previewCadence),
      annualSaveLabel: annualSaveLabel === "" ? null : annualSaveLabel,
      emailCreditsLabel: entry.emailCreditsLabel,
      smsCreditsLabel: entry.smsCreditsLabel,
      aiCreditsLabel: entry.aiCreditsLabel,
      cta: resolvePlanCardCta({
        cardPlanId: id,
        currentPlanId,
        isPilot: options.plan.isPilot,
        liveCadence,
        previewCadence: options.previewCadence,
      }),
    }
  })
}

export function buildPlanChangeConfirmCopy(options: {
  currentPlanId: ManagePlanId
  targetPlanId: ManagePlanId
  changeKind: PlanChangeKind
  previewCadence: BillingCadence
  renewalDateLabel: string | null
}): { title: string; body: string; primaryLabel: string; requiresPay: boolean } {
  const {
    currentPlanId,
    targetPlanId,
    changeKind,
    previewCadence,
    renewalDateLabel,
  } = options

  const requiresPay = changeKind === "convert" || changeKind === "upgrade"

  const title =
    changeKind === "convert"
      ? MANAGE_PLAN_COPY.confirmConvertTitle(targetPlanId)
      : changeKind === "upgrade"
        ? MANAGE_PLAN_COPY.confirmUpgradeTitle(targetPlanId)
        : changeKind === "plan-and-cadence"
          ? PLAN_RANK[targetPlanId] > PLAN_RANK[currentPlanId]
            ? MANAGE_PLAN_COPY.confirmUpgradeTitle(targetPlanId)
            : MANAGE_PLAN_COPY.confirmDowngradeTitle(targetPlanId)
          : changeKind === "downgrade"
            ? MANAGE_PLAN_COPY.confirmDowngradeTitle(targetPlanId)
            : MANAGE_PLAN_COPY.confirmCadenceTitle(previewCadence)

  return {
    title,
    body: requiresPay
      ? MANAGE_PLAN_COPY.confirmPayBody
      : MANAGE_PLAN_COPY.confirmScheduleBody(renewalDateLabel),
    primaryLabel: requiresPay
      ? MANAGE_PLAN_COPY.confirmPrimaryPay
      : MANAGE_PLAN_COPY.confirmPrimarySchedule,
    requiresPay,
  }
}

export function formatCurrentPlanSummary(plan: PlanSubscriptionSnapshot): string {
  if (plan.isPilot) {
    return `${plan.subscriptionPlan} · ${plan.renewalDateLabel ?? "Pilot"}`
  }
  const cadence =
    plan.billingCycle?.toLowerCase() === "annual" ? "year" : "month"
  const renewal = plan.renewalDateLabel ?? "—"
  return `${plan.planPriceNet}/${cadence} · ${renewal}`
}
