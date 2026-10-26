import Footer from "@/components/home/Footer"
import { CustomPlanCalculator } from "@/components/marketing/custom-plan/CustomPlanCalculator"
import { CustomPlanFaqs } from "@/components/marketing/custom-plan/CustomPlanFaqs"
import {
  CUSTOM_PLAN_HERO,
  CUSTOM_PLAN_SECTION_INSET,
  CUSTOM_PLAN_SECTION_Y,
} from "@/content/marketing/customPlanPage"
import { storedPlanFromQuote } from "@/lib/marketingCustomPlan/customPlanEstimate"
import { buildSignupPath } from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

/** Figma custom-plan calculator `6775:40023` — base package + packs. */
export default function CustomPlanPage() {
  return (
    <>
      <main
        className={cn(
          "flex w-full flex-col items-center overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-white",
          CUSTOM_PLAN_SECTION_INSET,
          CUSTOM_PLAN_SECTION_Y,
          "gap-[70px]",
        )}
      >
        <header className="flex w-full max-w-[656px] flex-col items-center gap-[22px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5 text-black">
            <p className="m-0 font-sans text-sm font-medium uppercase leading-normal">
              {CUSTOM_PLAN_HERO.eyebrow}
            </p>
            <h1 className="m-0 font-jakarta text-[34px] font-[500] leading-[40px] lg:text-[46px] lg:leading-[52px]">
              {CUSTOM_PLAN_HERO.title}
            </h1>
          </div>
          <p className="m-0 max-w-[596px] font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-lg lg:leading-[26px]">
            {CUSTOM_PLAN_HERO.body}
          </p>
        </header>

        <CustomPlanCalculator
          initialLocations={1}
          continueLabel="Continue to signup"
          continueHrefFor={({ quote, config }) =>
            buildSignupPath({
              plan: storedPlanFromQuote(quote.storedAsPlan),
              cadence: config.cadence,
              config,
            })
          }
        />
      </main>
      <CustomPlanFaqs />
      <Footer />
    </>
  )
}
