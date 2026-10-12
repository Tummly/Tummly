import { Link } from "react-router-dom"

import aiAssistantGlyph from "@/assets/svg/marketing/ai-assistant-glyph.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  AI_ASSISTANT_PLANS,
  AI_ASSISTANT_SECTION_BODY,
  AI_ASSISTANT_SECTION_HEADING,
  AI_ASSISTANT_SECTION_INSET,
  AI_ASSISTANT_SECTION_Y,
  type AiAssistantPlanStripCard,
} from "@/content/marketing/aiAssistantPage"
import { cn } from "@/lib/utils"

function PlanCard({ card }: { card: AiAssistantPlanStripCard }) {
  return (
    <div
      className={cn(
        "flex min-w-[280px] flex-1 flex-col gap-10 overflow-hidden py-6 lg:min-w-0",
        card.isHighlighted
          ? "rounded-[6px] border border-[#14a74a] bg-[rgba(20,167,74,0.05)] px-[26px]"
          : "border-[#cacaca] pr-[26px] lg:border-r lg:last:border-r-0 lg:last:pr-0",
      )}
    >
      <div className="flex flex-col gap-3">
        <h3 className="m-0 font-jakarta text-[30px] font-medium leading-none text-[#141414]">
          {card.name}
        </h3>
        <p className="m-0 max-w-[346px] text-sm font-normal leading-[19px] text-[#141414]">
          {card.description}
        </p>
      </div>

      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-2">
          <p className="m-0 text-[#141414]">
            <span className="text-[36px] font-normal leading-none">
              {card.priceAmount}
            </span>
            <span className="text-sm font-normal leading-none">
              {card.priceSuffix}
            </span>
          </p>
          <p className="m-0 text-sm font-normal leading-normal text-[#7c7c7c]">
            {card.subline}
          </p>
        </div>

        <MarketingButton
          asChild
          variant={card.isHighlighted ? "primary" : "secondary"}
          className="w-full"
        >
          <RequestTrialLink
            planIntent={
              card.id === "Pilot"
                ? { plan: "Pilot" }
                : { plan: card.id, cadence: "monthly" }
            }
          >
            {card.ctaLabel}
          </RequestTrialLink>
        </MarketingButton>
      </div>

      <div className="mt-auto flex items-center gap-2">
        <img
          src={aiAssistantGlyph}
          alt=""
          width={15}
          height={15}
          className="size-[15px] shrink-0"
          aria-hidden
        />
        <p className="m-0 text-sm font-normal leading-5 text-[#171717]">
          {card.aiAllowance}
        </p>
      </div>
    </div>
  )
}

/** Figma AI plans strip `5661:17526`. */
export function AiAssistantPlans() {
  return (
    <section className="w-full bg-[#f0f0f0]">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px]",
          AI_ASSISTANT_SECTION_INSET,
          AI_ASSISTANT_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[580px] flex-col gap-[18px]">
          <p className="m-0 text-base font-medium uppercase leading-normal text-black">
            {AI_ASSISTANT_PLANS.eyebrow}
          </p>
          <h2 className={cn("m-0", AI_ASSISTANT_SECTION_HEADING)}>
            {AI_ASSISTANT_PLANS.title}
          </h2>
          <p className={cn("m-0", AI_ASSISTANT_SECTION_BODY)}>
            {AI_ASSISTANT_PLANS.body}
          </p>
        </header>

        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 lg:mx-0 lg:w-full lg:overflow-visible lg:px-0">
          <div className="flex min-w-[960px] gap-[26px] lg:min-w-0">
            {AI_ASSISTANT_PLANS.cards.map((card) => (
              <PlanCard key={card.id} card={card} />
            ))}
          </div>
        </div>

        <MarketingButton asChild variant="secondary">
          <Link to={AI_ASSISTANT_PLANS.compareTo}>
            {AI_ASSISTANT_PLANS.compareLabel}
          </Link>
        </MarketingButton>
      </div>
    </section>
  )
}
