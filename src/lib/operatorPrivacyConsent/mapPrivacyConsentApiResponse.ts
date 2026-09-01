import { formatLocationsLastActivityAt } from "@/lib/operatorLocations/locationsPresentation"
import type { PermissionRecordsListApiRow } from "@/lib/operatorPrivacyConsent/permissionRecordsListQueryParams"
import {
  GUEST_PERMISSIONS_DEMO_CARDS,
  type GuestPermissionCard,
  type GuestPermissionId,
  type PermissionRecordRow,
  type PrivacyActivityItem,
  type PrivacySetupStatusRow,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"

export type PrivacyConsentPageApiData = {
  success?: boolean
  privacySetupRows: Array<{
    id: string
    requirement: string
    status: PrivacySetupStatusRow["status"]
  }>
  emailMarketingPermissionEnabled: boolean
  smsMarketingPermissionEnabled: boolean
  feedbackFollowUpPermissionEnabled: boolean
  smsConsentWording: string
  emailConsentWording: string
  privacyReady: boolean
  actorCanManage: boolean
  canViewGuests: boolean
}

export type PrivacyConsentActivityApiItem = {
  id: number
  locationId: number | null
  kind: string
  description: string | null
  occurredAt: string
}

const TOGGLE_FIELD_BY_PERMISSION_ID: Record<
  GuestPermissionId,
  keyof Pick<
    PrivacyConsentPageApiData,
    | "emailMarketingPermissionEnabled"
    | "smsMarketingPermissionEnabled"
    | "feedbackFollowUpPermissionEnabled"
  >
> = {
  "email-marketing": "emailMarketingPermissionEnabled",
  "sms-marketing": "smsMarketingPermissionEnabled",
  "feedback-follow-up": "feedbackFollowUpPermissionEnabled",
}

export function mapPrivacySetupRowsFromApi(
  rows: PrivacyConsentPageApiData["privacySetupRows"]
): PrivacySetupStatusRow[] {
  return rows.map((row) => ({
    id: row.id as PrivacySetupStatusRow["id"],
    requirement: row.requirement,
    status: row.status,
  }))
}

export function mapGuestPermissionCardsFromApi(
  data: Pick<
    PrivacyConsentPageApiData,
    | "emailMarketingPermissionEnabled"
    | "smsMarketingPermissionEnabled"
    | "feedbackFollowUpPermissionEnabled"
  >
): GuestPermissionCard[] {
  return GUEST_PERMISSIONS_DEMO_CARDS.map((template) => ({
    ...template,
    enabled: data[TOGGLE_FIELD_BY_PERMISSION_ID[template.id]],
  }))
}

export function mapPermissionRecordRowFromApi(
  row: PermissionRecordsListApiRow,
  now: Date
): PermissionRecordRow {
  const searchText = [
    row.guestName,
    row.permissionLabel,
    row.locationLabel,
    row.sourceLabel,
  ]
    .join(" ")
    .toLowerCase()

  return {
    id: row.id,
    locationGuestId: row.locationGuestId,
    guestName: row.guestName,
    permissionId: row.permissionId as GuestPermissionId,
    permissionLabel: row.permissionLabel,
    currentState: row.currentState,
    locationId: String(row.locationId),
    locationLabel: row.locationLabel,
    sourceLabel: row.sourceLabel,
    recordedLabel: formatLocationsLastActivityAt(row.recordedAt, now),
    searchText,
  }
}

export function mapPrivacyActivityItemFromApi(
  item: PrivacyConsentActivityApiItem,
  now: Date
): PrivacyActivityItem {
  return {
    id: String(item.id),
    timeLabel: formatLocationsLastActivityAt(item.occurredAt, now),
    description: item.description?.trim() ? item.description : "—",
  }
}

export function patchPayloadForGuestPermission(
  id: GuestPermissionId,
  enabled: boolean
): {
  emailMarketingPermissionEnabled?: boolean
  smsMarketingPermissionEnabled?: boolean
  feedbackFollowUpPermissionEnabled?: boolean
} {
  switch (id) {
    case "email-marketing":
      return { emailMarketingPermissionEnabled: enabled }
    case "sms-marketing":
      return { smsMarketingPermissionEnabled: enabled }
    case "feedback-follow-up":
      return { feedbackFollowUpPermissionEnabled: enabled }
  }
}
