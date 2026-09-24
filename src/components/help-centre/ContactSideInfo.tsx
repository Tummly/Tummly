import { ArrowRightIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"
import {
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_PRICING_PATH,
} from "@/constants/marketingNav"
import { cn } from "@/lib/utils"

const primaryCtaClass =
  "h-auto min-h-0 w-full gap-1.5 rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white shadow-none hover:bg-[#14a74a]/90 sm:w-auto"

const secondaryCtaClass =
  "h-auto min-h-11 w-full gap-1.5 rounded-[4px] border border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium leading-5 text-[#141414] shadow-none hover:border-[#707070] hover:bg-black/5 sm:w-auto"

const textLinkClass =
  "inline-flex h-auto min-h-0 w-fit self-start items-center justify-start gap-2 rounded-[4px] p-0 text-sm font-medium leading-5 text-[#141414] no-underline shadow-none hover:underline"

type ContactLinkBlock = {
  title: string
  body: string
  linkLabel: string
  to?: string
  placeholder?: boolean
}

const LINK_BLOCKS: ContactLinkBlock[] = [
  {
    title: "Ready to get started?",
    body: "The 30-day Pilot is £0, requires no payment card and does not renew automatically.",
    linkLabel: "Start Pilot",
    to: "/signup",
  },
  {
    title: "Plans and pricing",
    body: "You can compare current plans and allowances before sending your question.",
    linkLabel: "See pricing",
    to: MARKETING_PRICING_PATH,
  },
  {
    title: "Multi-Location",
    body: "Planning several Locations? See how Tummly supports restaurant groups.",
    linkLabel: "See Tummly for groups",
    to: MARKETING_PRICING_PATH,
  },
  {
    title: "Existing account",
    body: "Can’t sign in? Choose ‘Help with an existing Tummly account’ in this form.",
    linkLabel: "Log in for account support",
    to: "/login",
  },
]

export function ContactLinkBlocks({ className }: { className?: string }) {
  return (
    <div className={cn("flex w-full flex-col gap-[26px]", className)}>
      {LINK_BLOCKS.map((block, index) => (
        <div key={block.title} className="flex w-full flex-col gap-[26px]">
          {index > 0 && (
            <hr className="m-0 w-full border-0 border-t border-[#e5e5e5]" />
          )}
          <div className="flex flex-col gap-[26px]">
            <div className="flex flex-col gap-3">
              <h2 className="m-0 text-lg font-semibold text-black">
                {block.title}
              </h2>
              <p className="m-0 text-sm leading-[18px] text-[#141414] lg:text-base lg:leading-5">
                {block.body}
              </p>
            </div>
            {block.placeholder || !block.to ? (
              <span
                className={cn(textLinkClass, "cursor-default hover:no-underline")}
                aria-disabled="true"
              >
                {block.linkLabel}
                <ArrowRightIcon className="size-3.5" aria-hidden />
              </span>
            ) : (
              <Link to={block.to} className={textLinkClass}>
                {block.linkLabel}
                <ArrowRightIcon className="size-3.5" aria-hidden />
              </Link>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}

type ContactSideInfoProps = {
  className?: string
  /** Hide link blocks when they render separately (mobile stack). */
  showLinks?: boolean
}

export function ContactSideInfo({
  className,
  showLinks = true,
}: ContactSideInfoProps) {
  return (
    <div
      className={cn(
        "flex w-full flex-col gap-[50px] lg:max-w-[774px] lg:gap-[70px]",
        className
      )}
    >
      <div className="flex w-full flex-col gap-8">
        <div className="flex flex-col gap-[18px]">
          <h1 className="m-0 font-jakarta text-[34px] font-medium leading-normal text-black lg:text-[60px]">
            Tell us what you need.
          </h1>
          <p className="m-0 text-base leading-[22px] text-[#141414] lg:text-lg lg:leading-6">
            Restaurant guest? If your question is about using Tummly, choose
            &apos;Something else&apos;. For a privacy or data request, choose
            &apos;Privacy or data request&apos;. For food, service, orders,
            bookings or a restaurant&apos;s Offer terms, contact the restaurant
            directly.
          </p>
        </div>
        <div className="flex w-full flex-col gap-3.5 sm:flex-row sm:items-center sm:gap-5">
          <Button asChild className={primaryCtaClass}>
            <Link to={MARKETING_PRICING_PATH}>See pricing</Link>
          </Button>
          <Button asChild variant="outline" className={secondaryCtaClass}>
            <Link to={MARKETING_HOW_IT_WORKS_PATH}>See how Tummly works</Link>
          </Button>
        </div>
      </div>
      {showLinks && <ContactLinkBlocks />}
    </div>
  )
}

export { primaryCtaClass, secondaryCtaClass }
