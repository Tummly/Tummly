import type { ReactNode } from "react"

import Footer from "@/components/home/Footer"
import { cn } from "@/lib/utils"
import { marketingSectionInset } from "@/lib/marketing-layout"

type HelpCentrePageShellProps = {
  children: ReactNode
  className?: string
}

export function HelpCentrePageShell({
  children,
  className,
}: HelpCentrePageShellProps) {
  return (
    <>
      <div
        className={cn(
          "mx-auto w-full max-w-4xl py-10 sm:py-14 lg:py-16",
          marketingSectionInset,
          className
        )}
      >
        {children}
      </div>
      <Footer />
    </>
  )
}
