import type { LocationGuestMarketingPreference } from "@/types/dashboard"

export function isLocationGuestMarketingIneligible(
  preference: LocationGuestMarketingPreference | undefined
): boolean {
  return preference === "opted_out" || preference === "not_recorded"
}
