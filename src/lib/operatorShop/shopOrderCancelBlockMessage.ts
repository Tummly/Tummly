/**
 * Operator-facing copy when cancel is blocked (`canCancel` false).
 * Mirrors backend `CancelBlockReason` wire values.
 */
export function shopOrderCancelBlockMessage(
  reason: string | null | undefined
): string | null {
  switch (reason) {
    case "in_transit":
      return "This order has already been dispatched and cannot be cancelled."
    case "delivered":
      return "This order has been delivered and cannot be cancelled."
    case "production_started":
      return "Production has started for this order, so it cannot be cancelled here. Contact Tummly support if you need help."
    default:
      return null
  }
}
