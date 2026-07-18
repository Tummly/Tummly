/** Closed topic-tag vocabulary keys from Feedback AI classification. */
export type DetectedTagKey =
  | "FoodQuality"
  | "Service"
  | "WaitTime"
  | "Cleanliness"
  | "Value"
  | "Atmosphere"
  | "Billing"
  | "AllergiesDietary"
  | "BookingSeating"
  | "Other"

export const DETECTED_TAG_LABELS: Record<DetectedTagKey, string> = {
  FoodQuality: "Food quality",
  Service: "Service",
  WaitTime: "Wait time",
  Cleanliness: "Cleanliness",
  Value: "Value",
  Atmosphere: "Atmosphere",
  Billing: "Billing",
  AllergiesDietary: "Allergies & dietary",
  BookingSeating: "Booking & seating",
  Other: "Other",
}

export function labelForDetectedTag(key: string): string {
  if (key in DETECTED_TAG_LABELS) {
    return DETECTED_TAG_LABELS[key as DetectedTagKey]
  }
  return key
}
