import type { ReactNode } from "react"

import { cn } from "@/lib/utils"

import { AuthFooter } from "./AuthFooter"
import { AuthHeroPanel } from "./AuthHeroPanel"

interface AuthShellProps {
  children: ReactNode
  className?: string
}

/**
 * Split Auth chrome — form column left, lifestyle panel right (lg+).
 * Figma Marketing Website — Log in `4974:22028`.
 */
export function AuthShell({ children, className }: AuthShellProps) {
  return (
    <div
      className={cn(
        "flex h-dvh min-h-0 flex-col overflow-hidden bg-[#c9d1cb] p-0 lg:p-5",
        className
      )}
    >
      <div className="flex min-h-0 min-w-0 flex-1 overflow-hidden bg-[#fafafa] lg:rounded-xl">
        <div className="relative flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden lg:flex-[974]">
          <main className="relative z-[1] flex min-h-0 flex-1 flex-col overflow-y-auto px-5 pb-6 pt-10 sm:px-6 lg:px-10 lg:pb-10 lg:pt-[120px]">
            <div className="my-auto flex w-full flex-col items-center">
              <div className="flex w-full max-w-[468px] flex-col items-stretch">
                {children}
              </div>
            </div>
          </main>

          <AuthFooter />
        </div>

        <AuthHeroPanel />
      </div>
    </div>
  )
}

export { AuthFooter, AuthHeroPanel }
