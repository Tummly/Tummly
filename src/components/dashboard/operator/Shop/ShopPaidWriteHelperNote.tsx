import { Link } from "react-router-dom"
import type { ShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"
import { cn } from "@/lib/utils"

type ShopPaidWriteHelperNoteProps = {
  chrome: ShopPaidWriteChrome
  className?: string
}

export function ShopPaidWriteHelperNote({
  chrome,
  className,
}: ShopPaidWriteHelperNoteProps) {
  if (!chrome.purchaseDisabled || chrome.helperCta == null) {
    return null
  }

  return (
    <p className={cn("text-sm text-op-text-muted", className)}>
      Purchases are paused.{" "}
      <Link
        to={chrome.helperCta.href}
        className="font-medium text-op-action-primary underline-offset-2 hover:underline"
      >
        {chrome.helperCta.label}
      </Link>
    </p>
  )
}
