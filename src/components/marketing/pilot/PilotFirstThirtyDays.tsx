import { Link } from "react-router-dom"

import first30FeedbackIcon from "@/assets/svg/marketing/pilot/first-30-feedback.svg"
import first30PerformanceIcon from "@/assets/svg/marketing/pilot/first-30-performance.svg"
import first30QrIcon from "@/assets/svg/marketing/pilot/first-30-qr.svg"
import first30RelationshipsIcon from "@/assets/svg/marketing/pilot/first-30-relationships.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PILOT_FIRST_THIRTY_DAYS,
  PILOT_SECTION_BODY,
  PILOT_SECTION_INSET,
  type PilotFirstThirtyDaysCard,
} from "@/content/marketing/pilotPage"
import {
  buildSignupPath,
  saveSignupPlanIntent,
} from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

/** Figma `6476:16904` card glyphs (~29 CSS px). */
const CARD_ICONS: Record<
  PilotFirstThirtyDaysCard["id"],
  { src: string; width: number; height: number }
> = {
  qr: { src: first30QrIcon, width: 29, height: 29 },
  feedback: { src: first30FeedbackIcon, width: 29, height: 28 },
  relationships: { src: first30RelationshipsIcon, width: 31, height: 31 },
  performance: { src: first30PerformanceIcon, width: 29, height: 29 },
}

/**
 * Figma Pilot “Your first 30 days” `6476:16898` / cards `6476:16904`.
 * Centred header, four insight cards with Figma icons, dual CTAs.
 */
export function PilotFirstThirtyDays() {
  const signupTo = buildSignupPath(PILOT_FIRST_THIRTY_DAYS.planIntent)

  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="pilot-first-thirty-days-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[46px] lg:gap-[70px]",
          PILOT_SECTION_INSET,
          "py-12.5 lg:py-[70px]",
        )}
      >
        <header className="flex w-full max-w-[656px] flex-col items-center gap-[22px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5 text-black">
            <p className="m-0 font-sans text-sm font-medium uppercase leading-[normal] tracking-[1.12px]">
              {PILOT_FIRST_THIRTY_DAYS.eyebrow}
            </p>
            <h2
              id="pilot-first-thirty-days-heading"
              className="m-0 w-full max-w-[519px] font-jakarta text-[28px] font-medium leading-[normal] text-black lg:text-[36px]"
            >
              {PILOT_FIRST_THIRTY_DAYS.title}
            </h2>
          </div>
          <p className={cn("m-0 w-full max-w-[576px]", PILOT_SECTION_BODY)}>
            {PILOT_FIRST_THIRTY_DAYS.body}
          </p>
        </header>

        <div className="flex w-full flex-col items-center gap-[54px]">
          <div className="grid w-full grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {PILOT_FIRST_THIRTY_DAYS.cards.map((card) => {
              const icon = CARD_ICONS[card.id]
              return (
                <article
                  key={card.id}
                  className="flex h-full flex-col items-start gap-[22px] rounded-[8px] bg-white p-5"
                >
                  <img
                    src={icon.src}
                    alt=""
                    width={icon.width}
                    height={icon.height}
                    className="shrink-0 select-none"
                    decoding="async"
                    aria-hidden
                    draggable={false}
                  />
                  <div className="flex w-full flex-col items-start gap-3.5 text-[#141414]">
                    <h3 className="m-0 font-jakarta text-xl font-semibold leading-[normal]">
                      {card.title}
                    </h3>
                    <p className="m-0 font-sans text-sm font-normal leading-[18px]">
                      {card.body}
                    </p>
                  </div>
                </article>
              )
            })}
          </div>

          <div className="flex flex-wrap items-center justify-center gap-[22px]">
            <MarketingButton asChild variant="primary" className="w-[176px]">
              <Link
                to={signupTo}
                onClick={() => {
                  saveSignupPlanIntent(PILOT_FIRST_THIRTY_DAYS.planIntent)
                }}
              >
                {PILOT_FIRST_THIRTY_DAYS.primaryLabel}
              </Link>
            </MarketingButton>
            <MarketingButton asChild variant="secondary">
              <Link to={PILOT_FIRST_THIRTY_DAYS.secondaryTo}>
                {PILOT_FIRST_THIRTY_DAYS.secondaryLabel}
              </Link>
            </MarketingButton>
          </div>
        </div>
      </div>
    </section>
  )
}
