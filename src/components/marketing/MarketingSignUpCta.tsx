import { CircleCheckBig } from "lucide-react"
import { Link } from "react-router-dom"

import marketingArrowRight from "@/assets/svg/marketing-arrow-right.svg"
import { ctaLaunchBgPicture } from "@/assets/marketing-images"
import OptimizedImage from "@/components/media/OptimizedImage"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { Button } from "@/components/ui/button"
import { PANORAMIC_BG_IMAGE_SIZES } from "@/lib/imagePresets"
import {
  marketingHeroBody,
  marketingSectionHeading,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/** Figma Sign up instance — left-fade gradient over CTA photo. */
const SIGN_UP_GRADIENT =
  "linear-gradient(90deg, rgb(20, 20, 20) 23.523%, rgba(20, 20, 20, 0) 100%)"

const primaryButtonClass =
  "h-auto min-h-0 w-full gap-1.5 rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white shadow-none hover:bg-[#14a74a]/90 sm:w-[176px]"

const secondaryButtonClass =
  "h-auto min-h-0 w-full gap-1.5 rounded-[4px] border border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium leading-5 text-white shadow-none hover:border-[#707070] hover:bg-white/5 sm:w-[164px]"

export type MarketingSignUpCtaContent = {
  title: string
  body: string
  primaryLabel: string
  secondaryLabel: string
  secondaryTo: string
  badge: string
}

type MarketingSignUpCtaProps = {
  content: MarketingSignUpCtaContent
}

/**
 * Shared marketing bottom CTA (FAQ `4974:26424` / Trust `4974:26948`).
 * Reuses the shared CTA photo; copy comes from the page content module.
 */
export function MarketingSignUpCta({ content }: MarketingSignUpCtaProps) {
  return (
    <section className="w-full pt-5">
      <div className="relative flex h-auto min-h-105 w-full items-start justify-between overflow-hidden rounded-[8px] px-6.25 py-12.5 sm:min-h-120 lg:h-127.5 lg:px-15 lg:py-17.5">
        <div aria-hidden className="pointer-events-none absolute inset-0 rounded-[8px]">
          <div className="absolute inset-0 rounded-[8px] bg-[#f0f0f0]" />
          <div className="absolute inset-0 overflow-hidden rounded-[8px]">
            <OptimizedImage
              picture={ctaLaunchBgPicture}
              sizes={PANORAMIC_BG_IMAGE_SIZES}
              eager
              alt=""
              className="absolute top-[-33%] left-[21%] h-[196%] w-[79%] max-w-none object-cover"
            />
          </div>
          <div
            className="absolute inset-0 rounded-[8px]"
            style={{ backgroundImage: SIGN_UP_GRADIENT }}
          />
        </div>

        <div className="relative z-10 flex h-full min-h-80 w-full max-w-136 flex-col items-start justify-between gap-10 lg:min-h-0 lg:gap-0">
          <div className="flex flex-col gap-4.5">
            <h2
              className={cn(
                "m-0",
                marketingSectionHeading,
                "font-jakarta text-white",
              )}
            >
              {content.title}
            </h2>
            <p
              className={cn(
                "m-0 max-w-99.25",
                marketingHeroBody,
                "font-normal text-white lg:text-[18px] lg:leading-6",
              )}
            >
              {content.body}
            </p>
          </div>

          <div className="flex flex-col items-start gap-5">
            <div className="flex w-full flex-col items-stretch gap-5 sm:w-auto sm:flex-row sm:items-center">
              <Button asChild className={primaryButtonClass}>
                <RequestTrialLink>
                  {content.primaryLabel}
                  <img
                    src={marketingArrowRight}
                    alt=""
                    width={15}
                    height={10}
                    className="block size-auto h-2.5 w-3.75 shrink-0"
                    aria-hidden
                  />
                </RequestTrialLink>
              </Button>
              <Button asChild className={secondaryButtonClass}>
                <Link to={content.secondaryTo}>
                  {content.secondaryLabel}
                  <img
                    src={marketingArrowRight}
                    alt=""
                    width={15}
                    height={10}
                    className="block size-auto h-2.5 w-3.75 shrink-0"
                    aria-hidden
                  />
                </Link>
              </Button>
            </div>

            <div className="inline-flex items-center gap-2 rounded-[30px] bg-[#242424] px-2.5 py-2">
              <CircleCheckBig
                aria-hidden
                className="size-4 shrink-0 text-white"
                strokeWidth={2}
              />
              <p className="m-0 text-xs font-medium leading-normal text-[#fafafa]">
                {content.badge}
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
