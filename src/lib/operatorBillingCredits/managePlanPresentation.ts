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

export type PlanFeatureIcon = "email" | "sms" | "ai"

export type ManagePlanFeatureRow = {
  label: string
  value: string
  icon?: PlanFeatureIcon
}

export type ManagePlanCardViewModel = {
  id: ManagePlanId
  description: string
  priceAmount: string
  priceSuffix: string
  /** Combined string for tests and plain-text contexts. */
  priceHeadline: string
  priceSubline: string | null
  annualSaveLabel: string | null
  isMostPopular: boolean
  coreFeatures: ManagePlanFeatureRow[]
  allowanceFeatures: ManagePlanFeatureRow[]
  emailCreditsLabel: string
  smsCreditsLabel: string
  aiCreditsLabel: string
  cta: PlanCardCta
}

export type ManagePlanComparisonRow = {
  label: string
  values: Record<ManagePlanId, string>
}

export type ManagePlanFaqItem = {
  id: string
  question: string
  answerParagraphs: string[]
}

type PlanCatalogEntry = {
  description: string
  monthlyPriceNet: string
  annualPriceNet: string
  annualSaveLabel: string
  priceSublinePilot: string | null
  emailCreditsLabel: string
  smsCreditsLabel: string
  aiCreditsLabel: string
  isMostPopular: boolean
  coreFeatures: ManagePlanFeatureRow[]
  allowanceFeatures: ManagePlanFeatureRow[]
}

const PLAN_RANK: Record<ManagePlanId, number> = {
  Pilot: 0,
  Starter: 1,
  Growth: 2,
  Group: 3,
}

const INCLUDED = "Included"
const NOT_AVAILABLE = "Not available"
const AVAILABLE = "Available"
const QUALIFYING_KIT = "Qualifying kit included"

export const MANAGE_PLAN_CATALOG: Record<ManagePlanId, PlanCatalogEntry> = {
  Pilot: {
    description:
      "Try Guest Loop in one real Location before choosing a paid plan.",
    monthlyPriceNet: "£0",
    annualPriceNet: "£0",
    annualSaveLabel: "",
    priceSublinePilot: "No payment card required",
    emailCreditsLabel: "500 once",
    smsCreditsLabel: "20 once",
    aiCreditsLabel: "20 once",
    isMostPopular: false,
    coreFeatures: [
      { label: "Locations", value: "1" },
      { label: "Team users", value: "2" },
      { label: "Guest Form", value: "1 Published + 1 Draft" },
      { label: "Active QR placements", value: "Up to 5" },
      { label: "Private Feedback", value: INCLUDED },
      { label: "Guest list & permissions", value: INCLUDED },
      { label: "Campaigns", value: INCLUDED },
      { label: "Active Offers", value: "1" },
      { label: "Digital & self-print QR", value: INCLUDED },
      { label: "Physical starter kit", value: QUALIFYING_KIT },
    ],
    allowanceFeatures: [
      { label: "Email sends", value: "500 once", icon: "email" },
      { label: "SMS credits", value: "20 once", icon: "sms" },
      { label: "AI actions", value: "20 once", icon: "ai" },
      { label: "Top-ups", value: NOT_AVAILABLE },
    ],
  },
  Starter: {
    description:
      "For one Location that wants to hear from guests, build a guest list and bring people back.",
    monthlyPriceNet: "£39",
    annualPriceNet: "£398",
    annualSaveLabel: "Save £70 per year",
    priceSublinePilot: null,
    emailCreditsLabel: "2,500/month",
    smsCreditsLabel: "100/month",
    aiCreditsLabel: "100/month",
    isMostPopular: false,
    coreFeatures: [
      { label: "Locations", value: "1" },
      { label: "Team users", value: "3" },
      { label: "Guest Form", value: "1 Published + 1 Draft" },
      { label: "Active QR placements", value: "Up to 10" },
      { label: "Private Feedback & recovery", value: INCLUDED },
      { label: "Guest list & permissions", value: INCLUDED },
      { label: "Campaigns", value: INCLUDED },
      { label: "Active Offers", value: "Up to 3" },
      { label: "Digital & self-print QR", value: INCLUDED },
      { label: "Physical starter kit", value: QUALIFYING_KIT },
    ],
    allowanceFeatures: [
      { label: "Email sends", value: "2,500/month", icon: "email" },
      { label: "SMS credits", value: "100/month", icon: "sms" },
      { label: "AI actions", value: "100/month", icon: "ai" },
      { label: "Weekly Brief", value: INCLUDED },
      { label: "Top-ups", value: AVAILABLE },
      { label: "Support", value: "Standard Email" },
    ],
  },
  Growth: {
    description:
      "For active restaurants and small groups that need more reach and multi-Location visibility.",
    monthlyPriceNet: "£99",
    annualPriceNet: "£1,010",
    annualSaveLabel: "Save £178 per year",
    priceSublinePilot: null,
    emailCreditsLabel: "10,000/month",
    smsCreditsLabel: "350/month",
    aiCreditsLabel: "500/month",
    isMostPopular: true,
    coreFeatures: [
      { label: "Locations", value: "Up to 3" },
      { label: "Team users", value: "10" },
      { label: "Guest Forms", value: "1 Published + 1 Draft per Location" },
      { label: "Active QR placements", value: "Up to 25 per Location" },
      { label: "Private Feedback & recovery", value: INCLUDED },
      { label: "Guest list & permissions", value: INCLUDED },
      { label: "Campaigns", value: INCLUDED },
      { label: "Active Offers", value: "Up to 10 account-wide" },
      { label: "Multi-Location reporting", value: INCLUDED },
      { label: "Physical starter kit", value: QUALIFYING_KIT },
    ],
    allowanceFeatures: [
      { label: "Email sends", value: "10,000/month", icon: "email" },
      { label: "SMS credits", value: "350/month", icon: "sms" },
      { label: "AI actions", value: "500/month", icon: "ai" },
      { label: "Weekly Brief", value: "Location + consolidated" },
      { label: "Top-ups", value: AVAILABLE },
      { label: "Support", value: "Priority Email" },
    ],
  },
  Group: {
    description:
      "For growing restaurant groups that need more Locations, control and consolidated reporting.",
    monthlyPriceNet: "£199",
    annualPriceNet: "£2,030",
    annualSaveLabel: "Save £358 per year",
    priceSublinePilot: null,
    emailCreditsLabel: "25,000/month",
    smsCreditsLabel: "700/month",
    aiCreditsLabel: "1,500/month",
    isMostPopular: false,
    coreFeatures: [
      { label: "Locations", value: "Up to 5" },
      { label: "Team users", value: "25" },
      { label: "Guest Forms", value: "1 Published + 1 Draft per Location" },
      { label: "Active QR placements", value: "Up to 50 per Location" },
      { label: "Private Feedback & recovery", value: INCLUDED },
      { label: "Guest list & permissions", value: INCLUDED },
      { label: "Campaigns", value: INCLUDED },
      { label: "Active Offers", value: "Up to 25 account-wide" },
      { label: "Consolidated reporting", value: INCLUDED },
      { label: "Location-scoped permissions", value: INCLUDED },
    ],
    allowanceFeatures: [
      { label: "Email sends", value: "25,000/month", icon: "email" },
      { label: "SMS credits", value: "700/month", icon: "sms" },
      { label: "AI actions", value: "1,500/month", icon: "ai" },
      { label: "Weekly Brief", value: "Location + consolidated" },
      { label: "Top-ups", value: AVAILABLE },
      { label: "Support", value: "Priority + assisted onboarding" },
    ],
  },
}

export const MANAGE_PLAN_COMPARISON_ROWS: ManagePlanComparisonRow[] = [
  {
    label: "Locations",
    values: { Pilot: "1", Starter: "1", Growth: "Up to 3", Group: "Up to 5" },
  },
  {
    label: "Users",
    values: { Pilot: "2", Starter: "3", Growth: "10", Group: "25" },
  },
  {
    label: "Active QR placements",
    values: {
      Pilot: "Up to 5",
      Starter: "Up to 10",
      Growth: "Up to 25 per Location",
      Group: "Up to 50 per Location",
    },
  },
  {
    label: "Private Feedback",
    values: {
      Pilot: INCLUDED,
      Starter: INCLUDED,
      Growth: INCLUDED,
      Group: INCLUDED,
    },
  },
  {
    label: "Guest list & permissions",
    values: {
      Pilot: INCLUDED,
      Starter: INCLUDED,
      Growth: INCLUDED,
      Group: INCLUDED,
    },
  },
  {
    label: "Feedback recovery",
    values: {
      Pilot: "—",
      Starter: INCLUDED,
      Growth: INCLUDED,
      Group: INCLUDED,
    },
  },
  {
    label: "Campaigns",
    values: {
      Pilot: INCLUDED,
      Starter: INCLUDED,
      Growth: INCLUDED,
      Group: INCLUDED,
    },
  },
  {
    label: "Saved audiences",
    values: { Pilot: "—", Starter: "—", Growth: INCLUDED, Group: INCLUDED },
  },
  {
    label: "Active Offers",
    values: {
      Pilot: "1",
      Starter: "Up to 3",
      Growth: "Up to 10 account-wide",
      Group: "Up to 25 account-wide",
    },
  },
  {
    label: "Multi-Location reporting",
    values: { Pilot: "—", Starter: "—", Growth: INCLUDED, Group: INCLUDED },
  },
  {
    label: "Location-scoped permissions",
    values: { Pilot: "—", Starter: "—", Growth: "—", Group: INCLUDED },
  },
  {
    label: "Weekly Brief",
    values: {
      Pilot: "—",
      Starter: INCLUDED,
      Growth: "Location + consolidated",
      Group: "Location + consolidated",
    },
  },
  {
    label: "AI",
    values: {
      Pilot: "20 actions once",
      Starter: "100/month",
      Growth: "500/month",
      Group: "1,500/month",
    },
  },
  {
    label: "Email",
    values: {
      Pilot: "500 credits once",
      Starter: "2,500/month",
      Growth: "10,000/month",
      Group: "25,000/month",
    },
  },
  {
    label: "SMS",
    values: {
      Pilot: "20 credits once",
      Starter: "100/month",
      Growth: "350/month",
      Group: "700/month",
    },
  },
  {
    label: "Top-ups",
    values: {
      Pilot: NOT_AVAILABLE,
      Starter: AVAILABLE,
      Growth: AVAILABLE,
      Group: AVAILABLE,
    },
  },
  {
    label: "Support",
    values: {
      Pilot: "Pilot support",
      Starter: "Standard Email",
      Growth: "Priority Email",
      Group: "Priority + assisted onboarding",
    },
  },
]

export const MANAGE_PLAN_FAQ_ITEMS: ManagePlanFaqItem[] = [
  {
    id: "pilot-renew",
    question: "Does the Pilot renew automatically?",
    answerParagraphs: [
      "No. The 30-day Pilot does not automatically convert into a paid subscription and no payment card is required to start. At the end of the Pilot, you can choose a paid plan if you want to continue.",
    ],
  },
  {
    id: "pilot-start",
    question: "When does my 30-day Pilot start?",
    answerParagraphs: [
      "Your 30-day Pilot starts when you redeem the Activation Code from your starter kit, or when an authorised admin activates the Pilot.",
      "It does not start merely when an account is created.",
    ],
  },
  {
    id: "vat",
    question: "Is VAT included in the prices shown?",
    answerParagraphs: [
      "No. Public prices are shown excluding VAT. VAT is added where applicable.",
    ],
  },
  {
    id: "rollover",
    question: "Do unused monthly allowances roll over?",
    answerParagraphs: [
      "No. Included monthly AI, Email and SMS allowances do not roll over.",
      "Annual plans still release their included usage allowances monthly.",
    ],
  },
  {
    id: "pilot-topups",
    question: "Can Pilot accounts buy top-ups?",
    answerParagraphs: [
      "No. Top-ups are available only on active paid plans.",
    ],
  },
  {
    id: "allowance-exhausted",
    question: "What happens if I run out of an allowance?",
    answerParagraphs: [
      "Only the affected channel is paused.",
      "For example, using all of your SMS credits does not stop Email, AI or the rest of Guest Loop.",
      "Paid accounts can buy an eligible top-up or change plan where appropriate. Tummly does not create a negative balance or automatic post-paid overage.",
    ],
  },
  {
    id: "qr-materials",
    question: "Do I need to buy QR materials?",
    answerParagraphs: [
      "No.",
      "Tummly provides digital and self-print QR assets, and qualifying accounts can receive one physical starter-kit entitlement per Billing Account lifetime.",
      "Additional physical materials and normal reorders are paid separately through the Tummly Shop.",
    ],
  },
  {
    id: "starter-kit-per-location",
    question: "Is the starter kit included with every Location?",
    answerParagraphs: [
      "No.",
      "There is one qualifying free physical starter-kit entitlement per Billing Account lifetime. It is not repeated on every renewal, paid conversion or additional Location.",
    ],
  },
  {
    id: "pilot-to-paid",
    question: "What happens if I move from the Pilot to a paid plan?",
    answerParagraphs: [
      "Your existing Location, Guest Form, QR placements and history remain in place.",
      "You do not need to replace your existing QR materials simply because you move onto a paid plan.",
    ],
  },
  {
    id: "group-locations",
    question: "How many Locations can I run on Group?",
    answerParagraphs: [
      "Group includes up to 5 Locations.",
      "Additional Group Locations can be added for £39/month + VAT each, or £398/year + VAT on Annual.",
      "The self-serve Group limit is 30 Locations.",
    ],
  },
]

export const MANAGE_PLAN_COPY = {
  pageTitle: "Manage plan",
  pageSubtitle:
    "Choose the plan that fits your locations, message volume and support needs.",
  currentPlanHeading: "Current plan",
  mostPopular: "Most popular",
  cadenceMonthly: "Monthly",
  cadenceAnnual: "Annual",
  cadenceAnnualSave: "SAVE 15%",
  vatNotice:
    "Prices shown exclude VAT. VAT is added at checkout when applicable.",
  comparisonFeature: "Feature",
  completeFeaturesList: "Complete features list",
  capacityHeading: "Need more capacity without changing plan?",
  faqHeading: "Frequently asked questions",
  choosePlan: (plan: ManagePlanId) => `Choose ${plan}`,
  startPilot: "Start 30-day Pilot",
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

/** Layout classes for Manage plan surface (Figma 5754:110328 / 5754:111651). */
export const MANAGE_PLAN_PAGE_STACK_CLASS =
  "flex min-h-0 flex-1 flex-col gap-10"
export const MANAGE_PLAN_BREADCRUMB_CLASS =
  "flex items-center gap-2.5 text-base font-medium"
export const MANAGE_PLAN_BREADCRUMB_CURRENT_CLASS = "text-muted-foreground"
/** Season Mix stand-in: Roboto Serif (free hybrid with a light serif whisper). */
export const MANAGE_PLAN_DISPLAY_FONT_CLASS = "font-serif font-medium"
export const MANAGE_PLAN_BODY_STACK_CLASS = "flex flex-col gap-5"
export const MANAGE_PLAN_CURRENT_PLAN_CARD_CLASS =
  "flex flex-col overflow-clip rounded-[6px] border border-border bg-op-surface-primary p-6 dark:bg-op-color-gray-992"
export const MANAGE_PLAN_CURRENT_PLAN_NAME_CLASS =
  "m-0 font-serif text-[28px] font-medium leading-none text-foreground"
export const MANAGE_PLAN_CARDS_GRID_CLASS =
  "grid grid-cols-1 gap-8 xl:grid-cols-4 xl:gap-6.5"
export const MANAGE_PLAN_CARD_CLASS =
  "flex h-full min-w-0 flex-col gap-10 py-6"
export const MANAGE_PLAN_CARD_POPULAR_CLASS =
  "flex h-full min-w-0 flex-col gap-10 rounded-[6px] border border-primary bg-primary/[0.05] px-6.5 py-6"
export const MANAGE_PLAN_CARD_TITLE_CLASS =
  "m-0 font-serif text-[30px] font-medium leading-none text-foreground"
export const MANAGE_PLAN_CARD_DESCRIPTION_CLASS =
  "m-0 max-w-[346px] text-sm font-normal leading-[19px] text-foreground"
export const MANAGE_PLAN_FEATURE_ROW_CLASS =
  "flex items-center justify-between gap-3 text-sm whitespace-nowrap text-foreground"
export const MANAGE_PLAN_FEATURE_LABEL_CLASS = "shrink-0 font-medium"
export const MANAGE_PLAN_FEATURE_VALUE_CLASS = "shrink-0 text-right font-normal"
export const MANAGE_PLAN_PRICE_AMOUNT_CLASS =
  "font-serif text-[36px] font-medium leading-none text-foreground"
export const MANAGE_PLAN_PRICE_SUFFIX_CLASS =
  "font-sans text-sm font-medium leading-none text-foreground"
export const MANAGE_PLAN_SECTION_HEADING_CLASS =
  "m-0 font-serif text-[46px] font-medium leading-none text-foreground"
export const MANAGE_PLAN_FAQ_QUESTION_CLASS =
  "text-left font-serif text-[22px] font-medium leading-none text-foreground"
export const MANAGE_PLAN_FAQ_ANSWER_CLASS =
  "text-base font-normal leading-normal text-muted-foreground"
export const MANAGE_PLAN_CADENCE_SHELL_CLASS =
  "flex items-center gap-2.5 rounded-[6px] border border-border p-3"
export const MANAGE_PLAN_CADENCE_ITEM_CLASS =
  "inline-flex h-[41px] items-center justify-center gap-2.5 rounded px-3 text-sm font-medium text-muted-foreground transition-colors disabled:opacity-50"
export const MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS =
  "bg-[#202020] text-foreground"
export const MANAGE_PLAN_CADENCE_SAVE_BADGE_CLASS =
  "rounded-full bg-primary px-2.5 py-2 text-[10px] font-medium leading-none text-white"
export const MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS =
  "inline-flex items-center justify-center gap-1.5 text-base font-normal text-foreground hover:opacity-90"

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
  /** Soft lock / Dormant: pilot-restore keeps convert; dunning disables plan writes. */
  lockMode?: "none" | "pilot-restore" | "dunning"
}): PlanCardCta {
  const {
    cardPlanId,
    currentPlanId,
    isPilot,
    liveCadence,
    previewCadence,
    lockMode = "none",
  } = options

  if (cardPlanId === "Pilot" && !isPilot) {
    return {
      kind: "disabled",
      label: MANAGE_PLAN_COPY.startPilot,
      disabled: true,
    }
  }

  if (cardPlanId === currentPlanId) {
    if (
      !isPilot
      && liveCadence != null
      && previewCadence !== liveCadence
    ) {
      if (lockMode !== "none") {
        return {
          kind: "disabled",
          label:
            previewCadence === "annual"
              ? MANAGE_PLAN_COPY.switchToAnnual
              : MANAGE_PLAN_COPY.switchToMonthly,
          disabled: true,
        }
      }
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
    const convertLabel =
      cardPlanId === "Pilot"
        ? MANAGE_PLAN_COPY.startPilot
        : MANAGE_PLAN_COPY.choosePlan(cardPlanId)
    if (lockMode === "dunning") {
      return {
        kind: "disabled",
        label: convertLabel,
        disabled: true,
      }
    }
    return {
      kind: "action",
      label: convertLabel,
      disabled: false,
      changeKind,
    }
  }

  if (lockMode !== "none") {
    const isUpgrade =
      changeKind === "upgrade"
      || changeKind === "plan-and-cadence"
        && PLAN_RANK[cardPlanId] > PLAN_RANK[currentPlanId]
    return {
      kind: "disabled",
      label: isUpgrade
        ? MANAGE_PLAN_COPY.upgradeTo(cardPlanId)
        : MANAGE_PLAN_COPY.downgradeTo(cardPlanId),
      disabled: true,
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

function formatPriceParts(
  planId: ManagePlanId,
  cadence: BillingCadence
): { amount: string; suffix: string } {
  const entry = MANAGE_PLAN_CATALOG[planId]
  if (planId === "Pilot") {
    return { amount: entry.monthlyPriceNet, suffix: "/ for 30 days" }
  }
  if (cadence === "annual") {
    return { amount: entry.annualPriceNet, suffix: "/ year + VAT" }
  }
  return { amount: entry.monthlyPriceNet, suffix: "/ month + VAT" }
}

function formatPriceHeadline(
  planId: ManagePlanId,
  cadence: BillingCadence
): string {
  const { amount, suffix } = formatPriceParts(planId, cadence)
  return `${amount} ${suffix}`
}

function resolvePriceSubline(
  planId: ManagePlanId,
  previewCadence: BillingCadence
): string | null {
  const entry = MANAGE_PLAN_CATALOG[planId]
  if (planId === "Pilot") {
    return entry.priceSublinePilot
  }
  if (previewCadence === "monthly" && entry.annualSaveLabel !== "") {
    return entry.annualSaveLabel
  }
  return null
}

export function buildManagePlanCardViewModels(options: {
  plan: PlanSubscriptionSnapshot
  previewCadence: BillingCadence
  lockMode?: "none" | "pilot-restore" | "dunning"
}): ManagePlanCardViewModel[] {
  const currentPlanId = normalizePlanId(options.plan.subscriptionPlan)
  const liveCadence = liveCadenceFromSnapshot(options.plan)

  return MANAGE_PLAN_IDS.map((id) => {
    const entry = MANAGE_PLAN_CATALOG[id]
    const priceParts = formatPriceParts(id, options.previewCadence)
    const priceSubline = resolvePriceSubline(id, options.previewCadence)

    return {
      id,
      description: entry.description,
      priceAmount: priceParts.amount,
      priceSuffix: priceParts.suffix,
      priceHeadline: formatPriceHeadline(id, options.previewCadence),
      priceSubline,
      annualSaveLabel:
        id === "Pilot"
          ? null
          : options.previewCadence === "monthly"
            ? entry.annualSaveLabel || null
            : null,
      isMostPopular: entry.isMostPopular,
      coreFeatures: entry.coreFeatures,
      allowanceFeatures: entry.allowanceFeatures,
      emailCreditsLabel: entry.emailCreditsLabel,
      smsCreditsLabel: entry.smsCreditsLabel,
      aiCreditsLabel: entry.aiCreditsLabel,
      cta: resolvePlanCardCta({
        cardPlanId: id,
        currentPlanId,
        isPilot: options.plan.isPilot,
        liveCadence,
        previewCadence: options.previewCadence,
        lockMode: options.lockMode ?? "none",
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
    return `${plan.planPriceNet ?? "£0"}/ for 30 days · ${plan.renewalDateLabel ?? "Pilot"}`
  }
  const cadence =
    plan.billingCycle?.toLowerCase() === "annual" ? "year" : "month"
  const renewal = plan.renewalDateLabel ?? "—"
  return `${plan.planPriceNet}/${cadence} · ${renewal}`
}

export const GROUP_INCLUDED_LOCATIONS = 5

export const GROUP_LOCATION_CAP = 30

export type AdditionalGroupLocationViewModel = {
  includedCount: number
  extraCount: number
  totalCount: number
  cap: number
  canAdd: boolean
  canRemove: boolean
}

export type CancelPlanReason =
  | "too_expensive"
  | "not_enough_scans"
  | "not_using_campaigns"
  | "missing_feature"
  | "switched_provider"
  | "business_closed"
  | "other"

export const CANCEL_PLAN_REASON_OPTIONS: ReadonlyArray<{
  value: CancelPlanReason
  label: string
}> = [
  { value: "too_expensive", label: "Too expensive" },
  {
    value: "not_enough_scans",
    label: "Not enough scans or guest signups",
  },
  { value: "not_using_campaigns", label: "Not using campaigns" },
  { value: "missing_feature", label: "Missing feature" },
  { value: "switched_provider", label: "Switched provider" },
  { value: "business_closed", label: "Business closed" },
  { value: "other", label: "Other" },
]

export type CancelPlanDialogState = {
  open: boolean
  busy: boolean
  reason: CancelPlanReason | ""
  additionalNotes: string
  acknowledged: boolean
}

export const CANCEL_SUBSCRIPTION_DIALOG_COPY = {
  title: "Cancel subscription?",
  bodyPrimary:
    "Cancelling your subscription may affect access to campaigns, credits, QR materials, weekly briefs and account features after your billing period ends.",
  bodySecondary:
    "Your guest data, consent records, invoices and audit history may need to be retained according to your account and legal requirements.",
  reasonLabel: "Reason for cancellation",
  reasonPlaceholder: "Select a reason",
  notesLabel: "Additional notes",
  notesPlaceholder:
    "Tell us anything else that would help us understand your decision.",
  acknowledgment:
    "I understand that cancelling may affect campaigns, credits, QR materials, weekly briefs and account access after my billing period ends.",
  keepSubscription: "Keep subscription",
  continueCancellation: "Continue cancellation",
  contactSupport: "Contact support",
} as const

export function createInitialCancelPlanDialogState(): CancelPlanDialogState {
  return {
    open: false,
    busy: false,
    reason: "",
    additionalNotes: "",
    acknowledged: false,
  }
}

export function isCancelPlanDialogReady(state: CancelPlanDialogState): boolean {
  return state.reason !== "" && state.acknowledged
}

export type ManagePlanActionConfirmDialog = {
  open: boolean
  title: string
  body: string
  primaryLabel: string
  busy: boolean
}

export const ADDITIONAL_GROUP_LOCATION_COPY = {
  sectionTitle: "Additional Group Location",
  included: "Included",
  extra: "Extra",
  total: "Total",
  cap: "Cap",
  addLocation: "Add Location",
  removeLocation: "Remove Location",
  confirmAddTitle: "Add Location",
  confirmAddBody:
    "You will confirm this change on Tummly, then pay on Revolut. Your location allowance updates after payment succeeds.",
  confirmRemoveTitle: "Remove Location",
  confirmPrimaryPay: "Confirm and pay",
  confirmPrimarySchedule: "Confirm change",
  cancelPlan: "Cancel plan",
  confirmCancelTitle: "Cancel plan",
  confirmCancelBody: (renewalDateLabel: string | null) =>
    renewalDateLabel == null
      ? "Your access continues until your renewal date. The current period is not refunded."
      : `Your access continues until ${renewalDateLabel.replace(/^Renews /, "")}. The current period is not refunded.`,
  confirmCancelPrimary: "Confirm cancellation",
} as const

export function isCancelScheduled(plan: PlanSubscriptionSnapshot): boolean {
  return plan.scheduledChangeLine?.startsWith("Cancels on") ?? false
}

export function buildPlanRenewalDateMetric(
  plan: PlanSubscriptionSnapshot,
  labels: { renewalDate: string; cancelDate: string }
): { label: string; value: string } {
  if (isCancelScheduled(plan)) {
    return {
      label: labels.cancelDate,
      value: plan.scheduledChangeLine ?? plan.renewalDateLabel ?? "—",
    }
  }

  return {
    label: labels.renewalDate,
    value: plan.renewalDateLabel ?? "—",
  }
}

export function canRemoveExtraGroupLocation(
  includedLocations: number,
  activeLocations: number
): boolean {
  if (includedLocations <= GROUP_INCLUDED_LOCATIONS) {
    return false
  }

  return includedLocations - 1 >= activeLocations
}

export function buildAdditionalGroupLocationViewModel(
  plan: PlanSubscriptionSnapshot
): AdditionalGroupLocationViewModel | null {
  if (plan.subscriptionPlan !== "Group") {
    return null
  }

  const extraCount = Math.max(
    0,
    plan.includedLocations - GROUP_INCLUDED_LOCATIONS
  )

  return {
    includedCount: GROUP_INCLUDED_LOCATIONS,
    extraCount,
    totalCount: plan.includedLocations,
    cap: GROUP_LOCATION_CAP,
    canAdd: plan.includedLocations < GROUP_LOCATION_CAP,
    canRemove: canRemoveExtraGroupLocation(
      plan.includedLocations,
      plan.activeLocations
    ),
  }
}

export function buildExtraLocationRemoveConfirmCopy(
  renewalDateLabel: string | null
): { title: string; body: string; primaryLabel: string } {
  return {
    title: ADDITIONAL_GROUP_LOCATION_COPY.confirmRemoveTitle,
    body: MANAGE_PLAN_COPY.confirmScheduleBody(renewalDateLabel),
    primaryLabel: ADDITIONAL_GROUP_LOCATION_COPY.confirmPrimarySchedule,
  }
}

export function buildCancelPlanConfirmCopy(
  renewalDateLabel: string | null
): { title: string; body: string; primaryLabel: string } {
  return {
    title: ADDITIONAL_GROUP_LOCATION_COPY.confirmCancelTitle,
    body: ADDITIONAL_GROUP_LOCATION_COPY.confirmCancelBody(renewalDateLabel),
    primaryLabel: ADDITIONAL_GROUP_LOCATION_COPY.confirmCancelPrimary,
  }
}
