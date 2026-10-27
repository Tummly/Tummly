import type { VariantProps } from "class-variance-authority"

import type { badgeVariants } from "@/components/ui/badge"

type ShopPaymentBadgeVariant = NonNullable<
  VariantProps<typeof badgeVariants>["variant"]
>

const PAYMENT_LABEL_TO_STATUS: Record<string, string> = {
  paid: "paid",
  refunded: "refunded",
  free: "free",
  "payment failed": "payment_failed",
  "awaiting payment": "awaiting_payment",
}

/**
 * Shared `Badge` variant for an order's payment status. Free and
 * complimentary orders stay muted: no money was taken, so no green.
 */
export function shopPaymentBadgeVariant(input: {
  paymentStatus?: string | null
  paymentLabel?: string | null
  isComplimentary?: boolean
}): ShopPaymentBadgeVariant {
  if (input.isComplimentary === true) {
    return "tag"
  }

  const raw = input.paymentStatus?.trim().toLowerCase()
  const status =
    raw && raw.length > 0
      ? raw
      : PAYMENT_LABEL_TO_STATUS[input.paymentLabel?.trim().toLowerCase() ?? ""]

  switch (status) {
    case "paid":
      return "positive"
    case "payment_failed":
      return "negative"
    case "awaiting_payment":
      return "neutral"
    default:
      return "tag"
  }
}

/**
 * Express is no longer sold, but old orders keep their stored method, so the
 * sidebar reads it back instead of always saying "Standard".
 */
export function shopDeliveryMethodLabel(method?: string | null): string {
  return method?.trim().toLowerCase() === "express"
    ? "Express delivery"
    : "Standard delivery"
}
