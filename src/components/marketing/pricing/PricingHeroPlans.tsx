import { CheckIcon, ChevronDownIcon, CoinsIcon, MailIcon, MessageSquareIcon } from "lucide-react"
import type { MouseEvent } from "react"
import { Link } from "react-router-dom"

import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { Button } from "@/components/ui/button"
import {
  PRICING_CADENCE_COPY,
  PRICING_GROUP_BANNER,
  PRICING_HERO_HEADING,
  PRICING_PAGE_HERO,
  PRICING_PLAN_CARDS,
  PRICING_SECTION_BODY,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  pricingPlanPrice,
  type PricingFeatureRow,
  type PricingPlanCard,
} from "@/content/marketing/pricingPage"
import type { BillingCadence, PlanFeatureIcon } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  MANAGE_PLAN_CARD_CLASS,
  MANAGE_PLAN_CARD_DESCRIPTION_CLASS,
  MANAGE_PLAN_CARD_POPULAR_CLASS,
  MANAGE_PLAN_CARD_TITLE_CLASS,
  MANAGE_PLAN_CARDS_GRID_CLASS,
  MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS,
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_FEATURE_LABEL_CLASS,
  MANAGE_PLAN_FEATURE_ROW_CLASS,
  MANAGE_PLAN_FEATURE_VALUE_CLASS,
  MANAGE_PLAN_PRICE_AMOUNT_CLASS,
  MANAGE_PLAN_PRICE_SUFFIX_CLASS,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

/** Marketing cadence shell — Figma `5139:7672` (not Operator op-* tokens). */
const pricingCadenceShellClass =
  "flex items-center gap-2.5 rounded-[6px] border border-[rgba(38,38,38,0.21)] p-3"

const pricingCadenceItemClass =
  "inline-flex h-[41px] items-center justify-center gap-2.5 rounded px-3 text-sm font-medium text-[#7c7c7c] transition-colors"

/** Selected tab — Figma Main Bg `#202020` + white title. */
const pricingCadenceItemActiveClass = "bg-[#202020] text-white"

const pricingCadenceSaveBadgeClass =
  "rounded-full bg-[#14a74a] px-2.5 py-2 text-[10px] font-medium leading-none text-white"

const pilotButtonClass =
  "h-11 w-full shrink-0 rounded-[4px] border border-[#4e4e4e] bg-transparent text-sm font-medium text-[#141414] shadow-none hover:bg-[#141414]/5"

const popularButtonClass =
  "h-11 w-full shrink-0 rounded-[4px] bg-[#14a74a] text-sm font-medium text-white shadow-none hover:bg-[#14a74a]/90"

const groupPrimaryButtonClass =
  "h-auto min-h-0 rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-5 text-white shadow-none hover:bg-[#14a74a]/90"

const groupSecondaryClass =
  "rounded-sm text-sm font-medium text-[#141414] no-underline hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"

const groupContactButtonClass =
  "h-11 w-[203px] shrink-0 rounded-[4px] border border-[#4e4e4e] bg-transparent text-sm font-medium text-[#141414] shadow-none hover:bg-[#141414]/5"

function FeatureIcon({ icon }: { icon: PlanFeatureIcon }) {
  switch (icon) {
    case "email":
      return <MailIcon className="size-4 shrink-0" aria-hidden />
    case "sms":
      return <MessageSquareIcon className="size-4 shrink-0" aria-hidden />
    case "ai":
      return <CoinsIcon className="size-4 shrink-0" aria-hidden />
  }
}

function PlanFeatureRow({ row }: { row: PricingFeatureRow }) {
  return (
    <div
      className={cn(
        MANAGE_PLAN_FEATURE_ROW_CLASS,
        "max-lg:flex-wrap max-lg:whitespace-normal",
      )}
    >
      <div className="flex min-w-0 items-center gap-3">
        {row.icon != null ? <FeatureIcon icon={row.icon} /> : null}
        <p className={cn(MANAGE_PLAN_FEATURE_LABEL_CLASS, "max-lg:whitespace-normal")}>
          {row.label}
        </p>
      </div>
      <p
        className={cn(
          MANAGE_PLAN_FEATURE_VALUE_CLASS,
          "max-lg:whitespace-normal max-lg:shrink",
        )}
      >
        {row.value}
      </p>
    </div>
  )
}

function PricingPlanCardView({
  card,
  cadence,
}: {
  card: PricingPlanCard
  cadence: BillingCadence
}) {
  const price = pricingPlanPrice(card, cadence)
  const planIntent =
    card.id === "Pilot"
      ? ({ plan: "Pilot" } as const)
      : ({ plan: card.id, cadence } as const)

  return (
    <article
      className={
        card.isMostPopular
          ? MANAGE_PLAN_CARD_POPULAR_CLASS
          : MANAGE_PLAN_CARD_CLASS
      }
    >
      <div className="flex flex-col gap-3">
        <div className="flex min-h-9 items-center justify-between gap-3">
          <h3 className={MANAGE_PLAN_CARD_TITLE_CLASS}>{card.id}</h3>
          {card.isMostPopular ? (
            <span className="shrink-0 rounded bg-[#14a74a]/18 px-3 py-2 text-xs font-medium text-[#14a74a]">
              {MANAGE_PLAN_COPY.mostPopular}
            </span>
          ) : null}
        </div>
        <p className={cn(MANAGE_PLAN_CARD_DESCRIPTION_CLASS, "min-h-19")}>
          {card.description}
        </p>
      </div>

      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-2">
          <p className="m-0 text-[#141414]">
            <span className={MANAGE_PLAN_PRICE_AMOUNT_CLASS}>{price.amount}</span>
            <span className={MANAGE_PLAN_PRICE_SUFFIX_CLASS}>{price.suffix}</span>
          </p>
          <p className="m-0 min-h-10 text-sm font-medium leading-5 text-[#737373] lg:hidden">
            {price.sublineMobile}
          </p>
          <p className="m-0 hidden min-h-10 text-sm font-medium leading-5 text-[#737373] lg:block">
            {price.subline}
          </p>
        </div>

        <Button asChild className={card.isMostPopular ? popularButtonClass : pilotButtonClass}>
          <RequestTrialLink planIntent={planIntent}>{card.ctaLabel}</RequestTrialLink>
        </Button>

        <div className="flex flex-col gap-5.5">
          {card.coreFeatures.map((row) => (
            <PlanFeatureRow key={row.label} row={row} />
          ))}
          <hr className="border-[#e0e0e0]" />
          {card.allowanceFeatures.map((row) => (
            <PlanFeatureRow key={`${row.label}-${row.value}`} row={row} />
          ))}
        </div>
      </div>
    </article>
  )
}

function scrollToComparePlans(event: MouseEvent<HTMLAnchorElement>) {
  event.preventDefault()
  document
    .getElementById("compare-plans")
    ?.scrollIntoView({ behavior: "smooth", block: "start" })
}

type PricingHeroPlansProps = {
  cadence: BillingCadence
  onCadenceChange: (cadence: BillingCadence) => void
}

/** Figma Pricing hero + plan cards (`5139:4326` / mobile `4974:29544`). */
export function PricingHeroPlans({
  cadence,
  onCadenceChange,
}: PricingHeroPlansProps) {
  return (
    <section className="-mt-5 w-full bg-[#fafafa]">
      <div
        className={cn(
          "flex w-full flex-col gap-10",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex w-full flex-col items-start gap-[46px] lg:items-center">
          <div className="flex w-full max-w-[857px] flex-col items-start gap-[26px] text-left lg:mx-auto lg:items-center lg:text-center">
            <div className="flex w-full flex-col items-start gap-[18px] lg:items-center">
              <h1 className={cn("m-0 max-w-[857px]", PRICING_HERO_HEADING)}>
                {PRICING_PAGE_HERO.title}
              </h1>
              <p
                className={cn(
                  "m-0 max-w-[740px] lg:hidden",
                  PRICING_SECTION_BODY,
                )}
              >
                {PRICING_PAGE_HERO.bodyMobile}
              </p>
              <p
                className={cn(
                  "m-0 hidden max-w-[740px] lg:block",
                  PRICING_SECTION_BODY,
                )}
              >
                {PRICING_PAGE_HERO.body}
              </p>
            </div>
            <div className="hidden flex-wrap items-center justify-center gap-x-[35px] gap-y-2 lg:flex">
              {PRICING_PAGE_HERO.trustItems.map((item) => (
                <p
                  key={item}
                  className="m-0 flex items-center gap-2 text-xs font-medium leading-normal text-[#727272]"
                >
                  <CheckIcon className="size-3 shrink-0 text-[#14a74a]" aria-hidden />
                  {item}
                </p>
              ))}
            </div>
          </div>

          <div className="flex justify-start lg:justify-center">
            <div
              className={pricingCadenceShellClass}
              role="group"
              aria-label="Billing cadence"
            >
              <button
                type="button"
                aria-pressed={cadence === "monthly"}
                className={cn(
                  pricingCadenceItemClass,
                  "min-w-20",
                  cadence === "monthly" && pricingCadenceItemActiveClass,
                )}
                onClick={() => {
                  onCadenceChange("monthly")
                }}
              >
                {PRICING_CADENCE_COPY.monthly}
              </button>
              <button
                type="button"
                aria-pressed={cadence === "annual"}
                className={cn(
                  pricingCadenceItemClass,
                  cadence === "annual" && pricingCadenceItemActiveClass,
                )}
                onClick={() => {
                  onCadenceChange("annual")
                }}
              >
                {PRICING_CADENCE_COPY.annual}
                <span className={pricingCadenceSaveBadgeClass}>
                  {PRICING_CADENCE_COPY.saveBadge}
                </span>
              </button>
            </div>
          </div>
        </div>

        <div className={cn(MANAGE_PLAN_CARDS_GRID_CLASS, "order-1")}>
          {PRICING_PLAN_CARDS.map((card) => (
            <PricingPlanCardView key={card.id} card={card} cadence={cadence} />
          ))}
        </div>

        <div
          id="for-groups"
          className="order-2 flex scroll-mt-[180px] flex-col gap-8 rounded-[6px] bg-[#dfebe3] px-8 py-7 lg:order-3 lg:flex-row lg:items-center lg:justify-between lg:gap-6 lg:bg-[#f3fff7] lg:px-10 lg:py-8"
        >
          <div className="flex max-w-[720px] flex-col gap-[18px] lg:gap-3">
            <h2 className="m-0 font-jakarta text-[28px] font-medium leading-none text-[#141414]">
              {PRICING_GROUP_BANNER.title}
            </h2>
            <p className="m-0 text-base font-normal leading-6 text-[#141414] lg:hidden">
              {PRICING_GROUP_BANNER.bodyMobile}
            </p>
            <p className="m-0 hidden text-base font-normal leading-6 text-[#141414] lg:block">
              {PRICING_GROUP_BANNER.body}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-4">
            <Button asChild className={cn(groupContactButtonClass, "lg:hidden")}>
              <Link to={PRICING_GROUP_BANNER.secondaryTo}>
                {PRICING_GROUP_BANNER.secondaryLabel}
              </Link>
            </Button>
            <Button
              asChild
              className={cn(groupPrimaryButtonClass, "hidden lg:inline-flex")}
            >
              <RequestTrialLink
                planIntent={{ plan: "Group", cadence }}
              >
                {PRICING_GROUP_BANNER.primaryLabel}
              </RequestTrialLink>
            </Button>
            <Link
              to={PRICING_GROUP_BANNER.secondaryTo}
              className={cn(groupSecondaryClass, "hidden lg:inline")}
            >
              {PRICING_GROUP_BANNER.secondaryLabel}
            </Link>
          </div>
        </div>

        <div className="order-3 flex justify-center pt-2 lg:order-2">
          <a
            href="#compare-plans"
            className={MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS}
            onClick={scrollToComparePlans}
          >
            {PRICING_CADENCE_COPY.completeFeaturesList}
            <ChevronDownIcon className="size-3.5 shrink-0" aria-hidden />
          </a>
        </div>
      </div>
    </section>
  )
}
