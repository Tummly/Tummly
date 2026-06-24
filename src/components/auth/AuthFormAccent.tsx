import heroFormAccent from "@/assets/svg/hero-form-accent.svg"

import { cn } from "@/lib/utils"

type AuthFormAccentVariant = "auth" | "onboarding"

interface AuthFormAccentProps {
  className?: string
  /** Auth split shell (520/952) vs full-width onboarding shell (520/1728). */
  variant?: AuthFormAccentVariant
}

/** Figma node 557:2112 — light white fade over the line-art accent */
const AUTH_FORM_ACCENT_GRADIENT =
  "linear-gradient(25.34deg, rgb(255, 255, 255) 14%, rgba(255, 255, 255, 0.32) 58%), linear-gradient(92.01deg, rgb(255, 255, 255) 24%, rgba(255, 255, 255, 0.32) 52%)"

const ACCENT_WIDTH_CLASS: Record<AuthFormAccentVariant, string> = {
  /** Figma 557:1709 — 520px accent on the 952px auth form column */
  auth: "w-[54.58%] max-w-[520px]",
  /** Figma 920:1182 — 520px accent on the 1728px onboarding shell */
  onboarding: "w-[30.09%] max-w-[520px]",
}

/**
 * Decorative kitchen/food line-art on auth and onboarding form panels.
 */
export function AuthFormAccent({
  className,
  variant = "auth",
}: AuthFormAccentProps) {
  return (
    <div
      aria-hidden
      className={cn(
        "pointer-events-none absolute right-0 top-[-5px] z-0 aspect-[520/312] overflow-hidden",
        ACCENT_WIDTH_CLASS[variant],
        className
      )}
    >
      <img
        src={heroFormAccent}
        alt=""
        className="absolute inset-x-0 top-0 block h-[96.154%] w-full max-w-none"
      />
      <div
        className="absolute inset-x-0 left-0 top-0 h-full"
        style={{ backgroundImage: AUTH_FORM_ACCENT_GRADIENT }}
      />
    </div>
  )
}
