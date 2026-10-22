import type { SignupPlanPriceView } from "@/lib/signupPlanPrices"
import { cn } from "@/lib/utils"

type SignupPlanPriceProps = {
  price: SignupPlanPriceView | null
  amountClassName?: string
  sublineClassName?: string
}

/** Plan price block — amount + suffix, grey subline (Figma plan cards). */
export function SignupPlanPrice({
  price,
  amountClassName,
  sublineClassName,
}: SignupPlanPriceProps) {
  return (
    <div className="flex flex-col gap-2 text-[#141414]">
      <p className="m-0 whitespace-nowrap font-jakarta font-medium">
        <span className={cn("text-[36px] leading-normal", amountClassName)}>
          {price?.amount ?? "—"}
        </span>
        <span className="text-sm leading-normal">{price?.suffix ?? ""}</span>
      </p>
      <p
        className={cn(
          "m-0 font-sans text-sm font-medium leading-normal text-[#7c7c7c]",
          sublineClassName,
        )}
      >
        {price?.subline ?? "Loading price…"}
      </p>
    </div>
  )
}
