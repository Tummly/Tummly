import { Link } from "react-router-dom"

import creditsCoin from "@/assets/images/marketing/pricing/credits-coin.png"
import aiIcon from "@/assets/svg/marketing/pricing/usage/ai-icon.svg"
import creditsBadgeIcon from "@/assets/svg/marketing/pricing/usage/credits-badge-icon.svg"
import emailIcon from "@/assets/svg/marketing/pricing/usage/email-icon.svg"
import smsIcon from "@/assets/svg/marketing/pricing/usage/sms-icon.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRICING_HOW_USAGE_WORKS,
  PRICING_SECTION_BODY,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  type PricingHowUsageChannelId,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

const CHANNEL_ICONS = {
  ai: { src: aiIcon, width: 17, height: 17, className: "size-[17px] rotate-180" },
  email: { src: emailIcon, width: 18, height: 18, className: "size-[18px]" },
  sms: { src: smsIcon, width: 18, height: 18, className: "size-[18px]" },
} as const satisfies Record<
  PricingHowUsageChannelId,
  { src: string; width: number; height: number; className: string }
>

/** Figma How usage works `5661:14315` — after Compare plans. */
export function PricingHowUsageWorks() {
  return (
    <section
      id="usage-and-credits"
      aria-labelledby="pricing-how-usage-heading"
      className="w-full bg-[#f0f0f0]"
    >
      <div
        className={cn(
          "flex w-full flex-col gap-10 lg:flex-row lg:items-start lg:justify-center lg:gap-3",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[592px] shrink-0 flex-col gap-[46px]">
          <div className="flex max-w-[559px] flex-col gap-[22px]">
            <div className="flex flex-col gap-3.5">
              <p className="m-0 font-sans text-sm leading-4 font-normal tracking-[1.12px] text-[#4a4a4a] uppercase">
                {PRICING_HOW_USAGE_WORKS.eyebrow}
              </p>
              <h2
                id="pricing-how-usage-heading"
                className="m-0 max-w-[548px] font-jakarta text-[34px] font-semibold leading-[normal] text-black lg:text-[46px]"
              >
                {PRICING_HOW_USAGE_WORKS.title}
              </h2>
            </div>
            <p className={cn("m-0 max-w-[559px]", PRICING_SECTION_BODY)}>
              {PRICING_HOW_USAGE_WORKS.body}
            </p>
          </div>
          <MarketingButton asChild variant="dark" className="w-fit">
            <Link to={PRICING_HOW_USAGE_WORKS.ctaTo}>
              {PRICING_HOW_USAGE_WORKS.ctaLabel}
            </Link>
          </MarketingButton>
        </div>

        <div className="flex w-full min-w-0 flex-1 flex-col items-center gap-6 lg:max-w-[944px] lg:flex-row lg:items-center lg:gap-0">
          <img
            src={creditsCoin}
            alt=""
            width={176}
            height={188}
            className="h-auto w-[140px] shrink-0 object-contain sm:w-[176px]"
            decoding="async"
          />

          <div
            className="hidden h-px w-8 shrink-0 border-t border-dashed border-[#bdbdbd] lg:block"
            aria-hidden
          />

          <div className="flex min-w-0 flex-1 items-stretch">
            <div
              className="relative hidden w-0 shrink-0 lg:block"
              aria-hidden
            >
              <div className="absolute top-1/2 left-0 h-[171px] w-px -translate-y-1/2 border-l border-dashed border-[#bdbdbd]" />
            </div>

            <ul className="m-0 flex min-w-0 flex-1 list-none flex-col gap-3 p-0">
              {PRICING_HOW_USAGE_WORKS.channels.map((channel) => {
                const icon = CHANNEL_ICONS[channel.id]
                return (
                  <li key={channel.id} className="flex min-w-0 items-center">
                    <div
                      className="relative hidden h-px w-6 shrink-0 border-t border-dashed border-[#bdbdbd] lg:block"
                      aria-hidden
                    />
                    <article className="flex min-w-0 flex-1 items-start gap-4 overflow-hidden bg-[#e3e3e3] px-5 py-3.5">
                      <img
                        src={icon.src}
                        alt=""
                        width={icon.width}
                        height={icon.height}
                        className={cn("mt-0.5 shrink-0", icon.className)}
                        aria-hidden
                      />
                      <div className="flex min-w-0 flex-1 flex-col gap-2">
                        <div className="flex flex-wrap items-center gap-3">
                          <h3 className="m-0 font-jakarta text-sm font-semibold leading-[normal] text-[#262626]">
                            {channel.title}
                          </h3>
                          <span className="inline-flex items-center gap-1 rounded-[30px] bg-[#ccc] px-2 py-1.5">
                            <img
                              src={creditsBadgeIcon}
                              alt=""
                              width={12}
                              height={12}
                              className="size-3 shrink-0"
                              aria-hidden
                            />
                            <span className="font-jakarta text-[10px] font-medium leading-[normal] text-[#262626] sm:text-[11px]">
                              {channel.badge}
                            </span>
                          </span>
                        </div>
                        <p className="m-0 font-jakarta text-xs font-normal leading-[14px] text-[#262626]">
                          {channel.body}
                        </p>
                      </div>
                    </article>
                  </li>
                )
              })}
            </ul>
          </div>
        </div>
      </div>
    </section>
  )
}
