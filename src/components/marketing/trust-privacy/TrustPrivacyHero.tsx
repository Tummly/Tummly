import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { TRUST_PRIVACY_HERO } from "@/content/marketing/trustPrivacyPage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/** Trust hero title — *headline* Plus Jakarta Sans; 66 / 74 on desktop. */
const heroHeading =
  "font-jakarta text-[36px] font-medium leading-none tracking-normal lg:text-[66px] lg:leading-[74px]"

/** Trust hero body — *functional* Helvetica Neue 18 / 24. */
const heroBody =
  "font-sans text-base font-normal leading-[22px] tracking-normal lg:text-[18px] lg:leading-6"

/**
 * Figma Trust & privacy hero (`4974:26876`): #fafafa, pad 70/60,
 * title→body 18px, copy→CTAs 46px.
 * `-mt-5` cancels MainLayout header bottom chrome so nav and hero sit flush.
 */
export function TrustPrivacyHero() {
  return (
    <section className="-mt-5 w-full bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-11.5",
          marketingChromeContentInset,
          "py-17.5",
        )}
      >
        <div className="flex max-w-214.25 flex-col gap-4.5">
          <h1 className={cn("m-0 max-w-214 text-[#141414]", heroHeading)}>
            {TRUST_PRIVACY_HERO.title}
          </h1>
          <div
            className={cn(
              "flex max-w-168.5 flex-col gap-6 text-[#141414]",
              heroBody,
            )}
          >
            {TRUST_PRIVACY_HERO.paragraphs.map((paragraph) => (
              <p key={paragraph} className="m-0">
                {paragraph}
              </p>
            ))}
          </div>
        </div>

        <div className="flex w-full flex-col items-stretch gap-5.5 sm:w-auto sm:flex-row sm:items-start">
          <MarketingButton asChild variant="primary" className="w-full sm:w-auto">
            <Link to={TRUST_PRIVACY_HERO.primaryTo}>
              {TRUST_PRIVACY_HERO.primaryLabel}
            </Link>
          </MarketingButton>
          <MarketingButton
            asChild
            variant="secondary"
            className="w-full sm:w-auto"
          >
            <Link to={TRUST_PRIVACY_HERO.secondaryTo}>
              {TRUST_PRIVACY_HERO.secondaryLabel}
            </Link>
          </MarketingButton>
        </div>
      </div>
    </section>
  )
}
