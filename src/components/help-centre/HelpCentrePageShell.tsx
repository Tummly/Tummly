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
    <div className="flex w-full flex-1 flex-col bg-white">
      <div
        className={cn(
          "mx-auto flex w-full max-w-4xl flex-1 flex-col py-10 sm:py-14 lg:py-16",
          marketingSectionInset,
          className
        )}
      >
        {children}
      </div>
      <div className="mt-auto shrink-0">
        <Footer />
      </div>
    </div>
  )
}
