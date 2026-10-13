import googleG from "@/assets/svg/google-g.svg"
import microsoftSymbol from "@/assets/svg/microsoft-symbol.svg"
import { Button } from "@/components/ui/button"
import { buildExternalAuthStartUrl } from "@/lib/externalAuthStart"
import { cn } from "@/lib/utils"

type AuthSocialContinueButtonsProps = {
  /** `stack` matches Sign-in; `row` matches Signup Figma side-by-side. */
  layout?: "stack" | "row"
  /** `dark` matches marketing Sign up CTA on photo/video. */
  tone?: "light" | "dark"
  returnPath: string
  /** Runs before the browser leaves for the provider (e.g. stash plan intent). */
  onBeforeStart?: () => void
  /** Merged onto each continue button (e.g. Figma home hero `rounded-[8px]`). */
  className?: string
}

/**
 * Marketing auth chrome — Google / Microsoft continue actions.
 */
export function AuthSocialContinueButtons({
  layout = "stack",
  tone = "light",
  returnPath,
  onBeforeStart,
  className,
}: AuthSocialContinueButtonsProps) {
  const isRow = layout === "row"
  const isDark = tone === "dark"

  const buttonClass = cn(
    "h-11 min-h-11 gap-3 border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium leading-5",
    isDark
      ? "rounded-[8px] text-white hover:bg-white/10 hover:text-white"
      : "rounded-[4px] text-[#141414] hover:bg-[#f5f5f5]",
    isRow ? "w-full flex-1 sm:min-w-0" : "w-full",
    className,
  )

  const start = (provider: "google" | "microsoft") => {
    onBeforeStart?.()
    window.location.assign(buildExternalAuthStartUrl(provider, returnPath))
  }

  return (
    <div
      className={cn(
        "flex w-full",
        isRow
          ? "flex-col gap-2.5 sm:flex-row sm:items-start sm:gap-2.5"
          : "flex-col items-center gap-2.5",
      )}
    >
      <Button
        type="button"
        variant="outline"
        className={buttonClass}
        onClick={() => start("google")}
      >
        Continue with Google
        <img
          src={googleG}
          alt=""
          width={18}
          height={18}
          className="size-[18px] shrink-0"
        />
      </Button>

      <Button
        type="button"
        variant="outline"
        className={buttonClass}
        onClick={() => start("microsoft")}
      >
        Continue with Microsoft
        <img
          src={microsoftSymbol}
          alt=""
          width={18}
          height={18}
          className="size-[18px] shrink-0"
        />
      </Button>
    </div>
  )
}
