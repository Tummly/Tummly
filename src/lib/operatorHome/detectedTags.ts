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

export const DETECTED_TAG_KEYS = Object.keys(
  DETECTED_TAG_LABELS
) as DetectedTagKey[]

export function labelForDetectedTag(key: string): string {
  if (key in DETECTED_TAG_LABELS) {
    return DETECTED_TAG_LABELS[key as DetectedTagKey]
  }
  return key
}

export function isDetectedTagKey(key: string): key is DetectedTagKey {
  return key in DETECTED_TAG_LABELS
}

/** Stage a Detected Tag key with Other exclusivity. */
export function stageDetectedTagKey(
  draftTagKeys: readonly string[],
  key: string
): string[] {
  if (!isDetectedTagKey(key)) {
    return [...draftTagKeys]
  }
  if (key === "Other") {
    return ["Other"]
  }
  const withoutOther = draftTagKeys.filter((k) => k !== "Other")
  if (withoutOther.includes(key)) {
    return withoutOther
  }
  return [...withoutOther, key]
}

export function unstageDetectedTagKey(
  draftTagKeys: readonly string[],
  key: string
): string[] {
  return draftTagKeys.filter((k) => k !== key)
}

export function detectedTagSetsEqual(
  a: readonly string[],
  b: readonly string[]
): boolean {
  if (a.length !== b.length) {
    return false
  }
  const sortedA = [...a].sort()
  const sortedB = [...b].sort()
  return sortedA.every((key, index) => key === sortedB[index])
}

export function canApplyEditDetectedTags(input: {
  classificationStatus: "Pending" | "Succeeded" | "Failed"
  openTagKeys: readonly string[]
  draftTagKeys: readonly string[]
  draftSentiment: "positive" | "neutral" | "negative" | null
  saveStatus: "idle" | "saving" | "error"
}): boolean {
  if (input.saveStatus === "saving") {
    return false
  }
  if (input.classificationStatus === "Pending") {
    return false
  }
  if (input.classificationStatus === "Failed") {
    return input.draftSentiment != null
  }
  return !detectedTagSetsEqual(input.openTagKeys, input.draftTagKeys)
}
