/** Closed problem-theme vocabulary keys from Feedback AI classification. */
export type DetectedIssueKey =
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

export const DETECTED_ISSUE_LABELS: Record<DetectedIssueKey, string> = {
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

export function labelForDetectedIssue(key: string): string {
  if (key in DETECTED_ISSUE_LABELS) {
    return DETECTED_ISSUE_LABELS[key as DetectedIssueKey]
  }
  return key
}
