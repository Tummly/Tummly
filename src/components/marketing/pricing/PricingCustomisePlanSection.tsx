import { Link } from "react-router-dom"
import { ArrowRight } from "lucide-react"

import { Button } from "@/components/ui/button"
import { PRICING_CUSTOMISE_PLAN } from "@/content/marketing/pricingPage"

/** Figma Pricing customise CTA `6618:17381` — below Pricing plan cards. */
export function PricingCustomisePlanSection() {
  return (
    <section
      id="for-groups"
      aria-labelledby="pricing-customise-plan-heading"
      className="scroll-mt-[180px] w-full bg-white"
    >
      <div className="mx-auto w-full max-w-[1668px]">
        <div className="flex flex-col items-center gap-[42px] overflow-hidden rounded-lg bg-[#f4f4f4] px-8 py-12">
          <div className="flex w-full max-w-[548px] flex-col items-center gap-[18px] text-center text-black">
            <p className="font-sans text-sm leading-4 font-medium tracking-[1.12px] text-[#4a4a4a] uppercase">
              {PRICING_CUSTOMISE_PLAN.eyebrow}
            </p>
            <div className="flex w-full flex-col items-center gap-[22px]">
              <h2
                id="pricing-customise-plan-heading"
                className="font-jakarta text-4xl font-semibold leading-[normal]"
              >
                {PRICING_CUSTOMISE_PLAN.title}
              </h2>
              <p className="max-w-[532px] font-sans text-base font-normal leading-[22px]">
                {PRICING_CUSTOMISE_PLAN.body}
              </p>
            </div>
          </div>
          <Button
            asChild
            variant="default"
            className="h-auto gap-1.5 rounded bg-[#141414] px-[18px] py-4 text-sm font-medium text-white hover:bg-[#141414]/90"
          >
            <Link to={PRICING_CUSTOMISE_PLAN.ctaTo}>
              {PRICING_CUSTOMISE_PLAN.ctaLabel}
              <ArrowRight className="size-3.5" aria-hidden />
            </Link>
          </Button>
        </div>
      </div>
    </section>
  )
}
