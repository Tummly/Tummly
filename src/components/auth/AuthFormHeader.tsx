import { Link } from "react-router-dom"
import type { ReactNode } from "react"

import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { cn } from "@/lib/utils"

type AuthFormHeaderProps = {
  title: string
  description?: ReactNode
  descriptionClassName?: string
}

export function AuthFormHeader({
  title,
  description,
  descriptionClassName,
}: AuthFormHeaderProps) {
  return (
    <div className="flex w-full flex-col gap-10">
      <Link
        to="/"
        className="inline-flex w-fit shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"
      >
        <MarketingLogo
          onLight
          width={145}
          height={37}
          className="h-[37px] w-auto"
        />
      </Link>

      <div className="flex flex-col gap-3 text-[#141414]">
        <h1 className="m-0 font-serif text-[clamp(1.75rem,4vw,2.25rem)] font-medium leading-normal">
          {title}
        </h1>
        {description ? (
          <p
            className={cn(
              "m-0 text-base leading-[22px]",
              descriptionClassName
            )}
          >
            {description}
          </p>
        ) : null}
      </div>
    </div>
  )
}
