import { CoinsIcon, MailIcon, MessageSquareIcon } from "lucide-react"

import { PRICING_USAGE, PRICING_SECTION_INSET } from "@/content/marketing/pricingPage"
import type { PlanFeatureIcon } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  marketingHeroBody,
  marketingSectionHeading,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

function ChannelIcon({ icon }: { icon: PlanFeatureIcon }) {
  switch (icon) {
    case "email":
      return <MailIcon className="size-5 shrink-0" aria-hidden />
    case "sms":
      return <MessageSquareIcon className="size-5 shrink-0" aria-hidden />
    case "ai":
      return <CoinsIcon className="size-5 shrink-0" aria-hidden />
  }
}

const channelToneClass = {
  ai: "bg-[#f4fbff]",
  neutral: "bg-white",
  sms: "bg-[#f3fff7]",
} as const

/** Figma Usage + top-ups (`4974:29434`). */
export function PricingUsage() {
  return (
    <section className="w-full bg-[#f0f0f0]">
      <div
        className={cn(
          "mx-auto flex w-full max-w-[1668px] flex-col gap-8 py-[70px] lg:flex-row lg:items-start lg:gap-3",
          PRICING_SECTION_INSET,
        )}
      >
        <div className="flex w-full max-w-[592px] shrink-0 flex-col gap-[18px]">
          <h2
            className={cn(
              "m-0 font-medium text-black",
              marketingSectionHeading,
              "lg:text-[46px]",
            )}
          >
            {PRICING_USAGE.title}
          </h2>
          <p
            className={cn(
              "m-0 max-w-[525px] font-normal text-[#141414]",
              marketingHeroBody,
            )}
          >
            {PRICING_USAGE.body}
          </p>
        </div>

        <div className="flex min-w-0 flex-1 flex-col gap-3">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
            {PRICING_USAGE.channels.map((channel) => (
              <div
                key={channel.id}
                className={cn(
                  "flex flex-col gap-7 rounded p-6",
                  channelToneClass[channel.tone],
                )}
              >
                <div className="flex flex-col gap-3">
                  <div className="flex items-center gap-3">
                    <ChannelIcon icon={channel.icon} />
                    <p className="m-0 text-lg font-semibold text-[#262626]">
                      {channel.title}
                    </p>
                  </div>
                  <p className="m-0 text-base font-medium text-[#262626]">
                    {channel.definition}
                  </p>
                </div>
                <p className="m-0 text-sm font-normal italic text-[#262626]">
                  {channel.note}
                </p>
              </div>
            ))}
          </div>

          <div className="flex flex-col gap-[23px] overflow-hidden bg-white px-5 py-[27px] sm:px-[34px]">
            <div className="flex flex-col gap-2.5">
              <p className="m-0 text-lg font-bold text-[#262626]">
                {PRICING_USAGE.topUps.title}
              </p>
              <p className="m-0 max-w-[540px] text-sm font-normal leading-5 text-[#141414]">
                {PRICING_USAGE.topUps.body}
              </p>
            </div>

            {PRICING_USAGE.topUps.rows.map((row) => (
              <div key={row.id}>
                <hr className="mb-[23px] border-[#e0e0e0]" />
                <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:gap-6">
                  <div className="flex w-full max-w-[392px] items-center gap-3">
                    <ChannelIcon icon={row.icon} />
                    <p className="m-0 text-lg font-semibold text-[#262626]">
                      {row.label}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-x-8 gap-y-2 lg:gap-x-[100px]">
                    {row.packs.map((pack) => (
                      <p
                        key={pack}
                        className="m-0 py-1 text-sm font-normal text-[#262626]"
                      >
                        {pack}
                      </p>
                    ))}
                  </div>
                </div>
              </div>
            ))}

            <p className="m-0 text-sm font-normal text-[#262626]">
              {PRICING_USAGE.topUps.footnote}
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}
