import { CheckIcon, ChevronDownIcon, CoinsIcon, MailIcon, MessageSquareIcon } from "lucide-react"
import type { MouseEvent } from "react"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PRICING_CADENCE_COPY,
  PRICING_GROUP_BANNER,
  PRICING_HERO_HEADING,
  PRICING_PAGE_HERO,
  PRICING_PLAN_CARDS,
  PRICING_PLAN_CARD_CLASS,
  PRICING_PLAN_CARD_DESCRIPTION_CLASS,
  PRICING_PLAN_CARD_POPULAR_CLASS,
  PRICING_PLAN_CARD_TITLE_CLASS,
  PRICING_PLAN_CARDS_GRID_CLASS,
  PRICING_PLAN_FEATURE_LABEL_CLASS,
  PRICING_PLAN_FEATURE_ROW_CLASS,
  PRICING_PLAN_FEATURE_VALUE_CLASS,
  PRICING_PLAN_PRICE_AMOUNT_CLASS,
  PRICING_PLAN_PRICE_SUBLINE_CLASS,
  PRICING_PLAN_PRICE_SUFFIX_CLASS,
  PRICING_SECTION_BODY,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  pricingPlanPrice,
  type PricingFeatureRow,
  type PricingPlanCard,
} from "@/content/marketing/pricingPage"
import type { BillingCadence, PlanFeatureIcon } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS,
  MANAGE_PLAN_COPY,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

/** Marketing cadence shell — Figma `5139:7672` (not Operator op-* tokens). */
const pricingCadenceShellClass =
  "flex items-center gap-2.5 rounded-[6px] border border-[rgba(38,38,38,0.21)] p-3"

const pricingCadenceItemClass =
  "inline-flex h-[41px] cursor-pointer items-center justify-center gap-2.5 rounded px-3 text-sm font-medium text-[#7c7c7c] transition-colors hover:bg-[#efefef] hover:text-[#141414] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"

/** Selected tab — Figma Main Bg `#202020` + white title. */
const pricingCadenceItemActiveClass =
  "bg-[#202020] text-white hover:bg-[#2a2a2a] hover:text-white"

const pricingCadenceSaveBadgeClass =
  "rounded-full bg-[#14a74a] px-2.5 py-2 text-[10px] font-medium leading-none text-white"

const groupSecondaryClass =
  "rounded-sm text-sm font-medium text-[#141414] no-underline hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"

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
  const emphasizeLabel = row.label === "Starter Kit"
  return (
    <div
      className={cn(
        PRICING_PLAN_FEATURE_ROW_CLASS,
        "max-lg:flex-wrap max-lg:whitespace-normal",
      )}
    >
      <div className="flex min-w-0 items-center gap-3">
        {row.icon != null ? <FeatureIcon icon={row.icon} /> : null}
        <p
          className={cn(
            PRICING_PLAN_FEATURE_LABEL_CLASS,
            emphasizeLabel && "font-semibold",
            "max-lg:whitespace-normal",
          )}
        >
          {row.label}
        </p>
      </div>
      <p
        className={cn(
          PRICING_PLAN_FEATURE_VALUE_CLASS,
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

  const cardClass = card.isMostPopular
    ? PRICING_PLAN_CARD_POPULAR_CLASS
    : PRICING_PLAN_CARD_CLASS

  return (
    <article
      className={cn(cardClass, card.id === "Pilot" && "relative")}
    >
      {/* Straight rule after Pilot — Figma `5661:13765` (not border-radius). */}
      {card.id === "Pilot" ? (
        <span
          aria-hidden
          className="pointer-events-none absolute inset-y-0 -right-2 hidden w-px bg-[#e0e0e0] xl:block"
        />
      ) : null}
      <div className="flex flex-col gap-3">
        <div className="flex min-h-9 items-center justify-between gap-3">
          <h3 className={PRICING_PLAN_CARD_TITLE_CLASS}>{card.id}</h3>
          {card.isMostPopular ? (
            <span className="shrink-0 rounded bg-[#14a74a]/18 px-3 py-2 font-sans text-xs font-medium text-[#14a74a]">
              {MANAGE_PLAN_COPY.mostPopular}
            </span>
          ) : null}
        </div>
        <p className={cn(PRICING_PLAN_CARD_DESCRIPTION_CLASS, "min-h-19")}>
          {card.description}
        </p>
      </div>

      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-0.5">
          <p className="m-0 text-[#141414]">
            <span className={PRICING_PLAN_PRICE_AMOUNT_CLASS}>{price.amount}</span>
            <span className={PRICING_PLAN_PRICE_SUFFIX_CLASS}>{price.suffix}</span>
          </p>
          <p className={cn(PRICING_PLAN_PRICE_SUBLINE_CLASS, "lg:hidden")}>
            {price.sublineMobile}
          </p>
          <p className={cn(PRICING_PLAN_PRICE_SUBLINE_CLASS, "hidden lg:block")}>
            {price.subline}
          </p>
        </div>

        <MarketingButton
          asChild
          variant={card.isMostPopular ? "primary" : "secondary"}
          className="w-full shrink-0"
        >
          <RequestTrialLink planIntent={planIntent}>{card.ctaLabel}</RequestTrialLink>
        </MarketingButton>

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

/** Figma Pricing hero + plan cards (`5661:13671` / hero `5139:4326`). */
export function PricingHeroPlans({
  cadence,
  onCadenceChange,
}: PricingHeroPlansProps) {
  // Keep header chrome gap (`py-5`) so top radius reads on `#CBCBCB` — do not use `-mt-5`.
  return (
    <section className="w-full overflow-hidden rounded-tl-[8px] rounded-tr-[8px] bg-white">
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
                  <CheckIcon className="size-3 shrink-0 text-[#727272]" aria-hidden />
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

        <div className={cn(PRICING_PLAN_CARDS_GRID_CLASS, "order-1")}>
          {PRICING_PLAN_CARDS.map((card) => (
            <PricingPlanCardView key={card.id} card={card} cadence={cadence} />
          ))}
        </div>

        <div
          id="for-groups"
          className="order-2 flex scroll-mt-[180px] flex-col gap-8 rounded-[6px] bg-[#dfebe3] px-8 py-7 lg:order-3 lg:flex-row lg:items-center lg:justify-between lg:gap-6 lg:rounded-[8px] lg:px-10 lg:py-8"
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
            <MarketingButton
              asChild
              variant="secondary"
              className="w-[203px] shrink-0 lg:hidden"
            >
              <Link to={PRICING_GROUP_BANNER.secondaryTo}>
                {PRICING_GROUP_BANNER.secondaryLabel}
              </Link>
            </MarketingButton>
            <MarketingButton
              asChild
              variant="primary"
              className="hidden lg:inline-flex"
            >
              <RequestTrialLink planIntent={{ plan: "Group", cadence }}>
                {PRICING_GROUP_BANNER.primaryLabel}
              </RequestTrialLink>
            </MarketingButton>
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
