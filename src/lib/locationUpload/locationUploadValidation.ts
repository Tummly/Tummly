import { validationMessages } from "@/schemas/messages"

export const ukPostcodeRegex = /^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$/i

export type UploadedLocationDraft = {
  locationName: string
  address: string
  postcode: string
  addressOverridden: boolean
  locationPhone: string
  localContact: string
}

export type UploadedLocationStatus = "ready" | "missing_required" | "invalid_postcode"

export function combineLocalContact(name: string, email: string) {
  const trimmedName = name.trim()
  const trimmedEmail = email.trim()

  if (trimmedName && trimmedEmail) {
    return `${trimmedName} — ${trimmedEmail}`
  }

  return trimmedName || trimmedEmail
}

export function getUploadedLocationStatus(
  location: UploadedLocationDraft
): UploadedLocationStatus {
  const locationName = location.locationName.trim()
  const address = location.address.trim()
  const postcode = location.postcode.trim()

  if (!locationName || !address || !postcode) {
    return "missing_required"
  }

  if (!ukPostcodeRegex.test(postcode)) {
    return "invalid_postcode"
  }

  return "ready"
}

export function getUploadedLocationStatusLabel(status: UploadedLocationStatus) {
  switch (status) {
    case "ready":
      return "Ready"
    case "missing_required":
      return "Missing required field"
    case "invalid_postcode":
      return validationMessages.accountSetup.postcode.invalid
  }
}

export function countReadyUploadedLocations(locations: UploadedLocationDraft[]) {
  return locations.filter(
    (location) => getUploadedLocationStatus(location) === "ready"
  ).length
}

export function areAllUploadedLocationsReady(locations: UploadedLocationDraft[]) {
  return (
    locations.length > 0 &&
    countReadyUploadedLocations(locations) === locations.length
  )
}
