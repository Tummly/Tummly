import type { ReactNode } from "react"

import { AuthFooter } from "./AuthFooter"
import { AuthFormAccent } from "./AuthFormAccent"
import { AuthHeroPanel } from "./AuthHeroPanel"

interface AuthShellProps {
  children: ReactNode
}

export function AuthShell({ children }: AuthShellProps) {
  return (
    <div className="flex h-dvh min-h-0 flex-col overflow-hidden bg-white lg:flex-row">
      <AuthHeroPanel />

      <div className="relative flex min-h-0 flex-1 flex-col overflow-hidden lg:flex-[952] lg:max-w-[min(100%,952px)]">
        <AuthFormAccent className="hidden lg:block" />

        <main className="relative z-[1] flex min-h-0 flex-1 flex-col items-center justify-center overflow-y-auto px-5 py-6 sm:px-6 lg:px-[clamp(1.5rem,12vw,13.125rem)] lg:py-4">
          {children}
        </main>

        <AuthFooter />
      </div>
    </div>
  )
}

export { AuthFormAccent, AuthFooter, AuthHeroPanel }
