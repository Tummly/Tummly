import { ArrowRightIcon } from "lucide-react"

import {
  PRICING_GUEST_LOOP,
  PRICING_SECTION_BODY,
  PRICING_SECTION_HEADING,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

/** Figma Guest Loop strip (`5437:13673` / `5141:8494`). */
export function PricingGuestLoop() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col gap-10 lg:flex-row lg:items-center lg:justify-between lg:gap-[60px]",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex max-w-[548px] flex-col gap-[18px]">
          <h2 className={cn("m-0", PRICING_SECTION_HEADING)}>
            {PRICING_GUEST_LOOP.title}
          </h2>
          <p className={cn("m-0", PRICING_SECTION_BODY)}>
            {PRICING_GUEST_LOOP.body}
          </p>
        </div>

        <div className="flex flex-col gap-6 sm:flex-row sm:flex-wrap sm:items-start sm:justify-center sm:gap-5 lg:gap-10">
          {PRICING_GUEST_LOOP.steps.map((step, index) => (
            <div key={step.id} className="flex items-center gap-5">
              <div className="flex min-w-[120px] flex-col items-center gap-3 text-center text-[#141414]">
                <p className="m-0 text-lg font-medium leading-6">{step.label}</p>
                <p className="m-0 whitespace-pre-line text-sm font-normal leading-5">
                  {step.detail}
                </p>
              </div>
              {index < PRICING_GUEST_LOOP.steps.length - 1 ? (
                <ArrowRightIcon
                  className="hidden size-4 shrink-0 text-[#b0b0b0] sm:block"
                  aria-hidden
                />
              ) : null}
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
