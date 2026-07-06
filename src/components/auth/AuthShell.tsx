import type { ReactNode } from "react"

import { cn } from "@/lib/utils"

import { AuthFooter } from "./AuthFooter"
import { AuthFormAccent } from "./AuthFormAccent"
import { AuthHeroPanel } from "./AuthHeroPanel"

interface AuthShellProps {
  children: ReactNode
  className?: string
}

export function AuthShell({ children, className }: AuthShellProps) {
  return (
    <div
      className={cn(
        "flex h-dvh min-h-0 flex-col overflow-hidden bg-white lg:flex-row",
        className
      )}
    >
      <AuthHeroPanel />

      <div className="relative flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden lg:flex-[952]">
        <AuthFormAccent />

        <main className="relative z-[1] flex min-h-0 flex-1 flex-col overflow-y-auto px-5 py-6 sm:px-6 lg:px-[clamp(1.5rem,12vw,13.125rem)] lg:py-4">
          <div className="my-auto flex w-full flex-col items-center">
            {children}
          </div>
        </main>

        <AuthFooter />
      </div>
    </div>
  )
}

export { AuthFormAccent, AuthFooter, AuthHeroPanel }
