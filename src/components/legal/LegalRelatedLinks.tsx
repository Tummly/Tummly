import { Link } from "react-router-dom"

import marketingArrowRight from "@/assets/svg/marketing-arrow-right.svg"
import {
  LEGAL_RELATED_LINKS,
  type LegalRelatedLink,
} from "@/content/legal/legalRelatedLinks"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const linkTitle =
  "font-jakarta text-[20px] font-medium leading-normal text-[#141414] lg:text-[22px]"

const linkDescription =
  "text-base font-normal leading-normal text-[#141414]"

const linkActionClass =
  "inline-flex shrink-0 items-center gap-2 rounded-[4px] text-sm font-medium leading-5 text-[#141414] no-underline transition-colors hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"

function LegalLinkRow({ link }: { link: LegalRelatedLink }) {
  const action = (
    <>
      {link.linkLabel}
      <img
        src={marketingArrowRight}
        alt=""
        width={15}
        height={10}
        className="block size-auto h-2.5 w-3.75 shrink-0 brightness-0"
        aria-hidden
      />
    </>
  )

  return (
    <div className="flex w-full flex-col items-start gap-4 sm:flex-row sm:items-center sm:gap-5">
      <div className="flex min-w-0 flex-1 flex-col gap-3">
        <h3 className={cn("m-0", linkTitle)}>{link.title}</h3>
        <p className={cn("m-0", linkDescription)}>{link.description}</p>
      </div>

      {link.href.kind === "route" ? (
        <Link to={link.href.to} className={linkActionClass}>
          {action}
        </Link>
      ) : (
        <span
          className={cn(linkActionClass, "cursor-default hover:no-underline")}
          aria-disabled="true"
        >
          {action}
        </span>
      )}
    </div>
  )
}

type LegalRelatedLinksProps = {
  /** Optional section heading above the link rows. */
  title?: string
  links?: readonly LegalRelatedLink[]
  className?: string
}

/**
 * Related legal document rows (Figma Privacy Notice `4974:26980`).
 * Default background is muted `#f0f0f0` as on legal pages.
 */
export function LegalRelatedLinks({
  title,
  links = LEGAL_RELATED_LINKS,
  className,
}: LegalRelatedLinksProps) {
  return (
    <section className={cn("w-full bg-[#f0f0f0]", className)}>
      <div
        className={cn(
          "mx-auto flex w-full flex-col",
          marketingChromeContentInset,
          "py-17.5",
          title ? "gap-15" : "gap-7.5",
        )}
      >
        {title ? (
          <h2
            className={cn(
              "m-0 max-w-137 font-jakarta text-[34px] font-medium leading-normal text-[#141414] lg:text-[46px]",
            )}
          >
            {title}
          </h2>
        ) : null}

        <div className="flex w-full flex-col gap-7.5">
          {links.map((link, index) => (
            <div key={link.id} className="flex w-full flex-col gap-7.5">
              {index > 0 ? (
                <div aria-hidden className="h-px w-full bg-[#d4d4d4]" />
              ) : null}
              <LegalLinkRow link={link} />
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
