import { parseApiInstantMs } from "@/lib/operatorHome/relativeTime"
import type {
  OperatorGuestFixture,
  OperatorGuestSmartGroupId,
} from "@/types/operatorGuests"

const DAY_MS = 24 * 60 * 60 * 1000

export function isWithinDaysUtc(
  iso: string,
  days: number,
  nowMs: number
): boolean {
  const thenMs = parseApiInstantMs(iso)
  if (Number.isNaN(thenMs)) {
    return false
  }
  return nowMs - thenMs <= days * DAY_MS
}

export function isOlderThanDaysUtc(
  iso: string,
  days: number,
  nowMs: number
): boolean {
  const thenMs = parseApiInstantMs(iso)
  if (Number.isNaN(thenMs)) {
    return false
  }
  return nowMs - thenMs > days * DAY_MS
}

export function guestMatchesSmartGroup(
  guest: OperatorGuestFixture,
  smartGroupId: OperatorGuestSmartGroupId,
  nowMs: number
): boolean {
  switch (smartGroupId) {
    case "all-guests":
      return true
    case "new-guests":
      return isWithinDaysUtc(guest.capturedAt, 13, nowMs)
    case "needs-recovery":
      return guest.needsRecovery
    case "positive-feedback":
      return guest.latestFeedbackSentiment === "positive"
    case "offer-not-redeemed":
      return guest.hasOffer && guest.offerRedeemedAt == null
    case "recent-redeemers":
      return (
        guest.offerRedeemedAt != null &&
        isWithinDaysUtc(guest.offerRedeemedAt, 13, nowMs)
      )
    case "dormant-guests":
      return (
        guest.lastInteractionAt != null &&
        isOlderThanDaysUtc(guest.lastInteractionAt, 90, nowMs)
      )
    default:
      return false
  }
}

export function filterGuestsBySmartGroup(
  guests: readonly OperatorGuestFixture[],
  smartGroupId: OperatorGuestSmartGroupId,
  nowMs: number
): OperatorGuestFixture[] {
  return guests.filter((guest) =>
    guestMatchesSmartGroup(guest, smartGroupId, nowMs)
  )
}

export function countGuestsBySmartGroup(
  guests: readonly OperatorGuestFixture[],
  smartGroupId: OperatorGuestSmartGroupId,
  nowMs: number
): number {
  return filterGuestsBySmartGroup(guests, smartGroupId, nowMs).length
}
