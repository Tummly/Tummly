import type { ReactNode } from "react"

import {
  ContactLinkBlocks,
  ContactSideInfo,
} from "@/components/help-centre/ContactSideInfo"
import Footer from "@/components/home/Footer"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type ContactPageShellProps = {
  panel: ReactNode
  className?: string
}

/**
 * Marketing Contact / Success shell.
 * Matches full Figma Contact frame (`5008:56233`): `#fafafa` content card flush
 * under the header (`-mt-5`), then Footer as a sibling so `pt-5` reveals the
 * `#CBCBCB` chrome gap above the footer links.
 */
export function ContactPageShell({ panel, className }: ContactPageShellProps) {
  return (
    <>
      <div
        className={cn(
          "-mt-5 w-full rounded-bl-[8px] rounded-br-[8px] bg-[#fafafa]",
          className
        )}
      >
        <div
          className={cn(
            "mx-auto flex w-full flex-col gap-[50px] py-6",
            "lg:flex-row lg:items-start lg:justify-center lg:gap-[70px] lg:py-[70px]",
            marketingChromeContentInset
          )}
        >
          <ContactSideInfo
            showLinks={false}
            className="w-full shrink-0 lg:hidden"
          />
          <ContactSideInfo
            showLinks
            className="hidden w-full max-w-[774px] shrink-0 lg:flex"
          />
          <div className="w-full shrink-0 lg:w-[724px] lg:max-w-[724px]">
            <div className="flex w-full flex-col gap-10 overflow-hidden rounded-[2px] bg-[#f2f2f2] p-5 lg:gap-[70px] lg:p-10">
              {panel}
            </div>
          </div>
          <ContactLinkBlocks className="w-full lg:hidden" />
        </div>
      </div>
      <Footer />
    </>
  )
}
