import googleG from "@/assets/svg/google-g.svg"
import microsoftSymbol from "@/assets/svg/microsoft-symbol.svg"
import { Button } from "@/components/ui/button"
import { buildExternalAuthStartUrl } from "@/lib/externalAuthStart"
import { cn } from "@/lib/utils"

type AuthSocialContinueButtonsProps = {
  /** `stack` matches Sign-in; `row` matches Signup Figma side-by-side. */
  layout?: "stack" | "row"
  returnPath: string
}

/**
 * Marketing auth chrome — Google / Microsoft continue actions.
 */
export function AuthSocialContinueButtons({
  layout = "stack",
  returnPath,
}: AuthSocialContinueButtonsProps) {
  const isRow = layout === "row"

  return (
    <div
      className={cn(
        "flex w-full",
        isRow
          ? "flex-col gap-2.5 sm:flex-row sm:items-start sm:gap-2.5"
          : "flex-col items-center gap-2.5"
      )}
    >
      <Button
        type="button"
        variant="outline"
        className={cn(
          "h-11 min-h-11 gap-3 rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium leading-5 text-[#141414] hover:bg-[#f5f5f5]",
          isRow ? "w-full flex-1 sm:min-w-0" : "w-full"
        )}
        onClick={() => {
          window.location.assign(
            buildExternalAuthStartUrl("google", returnPath)
          )
        }}
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
        className={cn(
          "h-11 min-h-11 gap-3 rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium leading-5 text-[#141414] hover:bg-[#f5f5f5]",
          isRow ? "w-full flex-1 sm:min-w-0" : "w-full"
        )}
        onClick={() => {
          window.location.assign(
            buildExternalAuthStartUrl("microsoft", returnPath)
          )
        }}
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
