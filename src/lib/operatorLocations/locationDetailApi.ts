import type { LocationLifecycleStatus } from "@/lib/operatorLocations/locationsPresentation"
import type { LocationSetupStatus } from "@/lib/operatorLocations/locationsPresentation"
import type {
  LocationSetupChecklistItemId,
  LocationSetupChecklistStatusId,
  LocationDetailTeamAccessRow,
} from "@/lib/operatorLocations/locationDetailPresentation"
import { formatAccessActivityOccurredAt } from "@/lib/operatorTeamPermissions/teamPermissionsPresentation"

export type LocationDetailApiTeamAccessRow = {
  membershipId: number
  userId: number
  name: string
  role: string
  accessLabel: string
  lastActiveAt: string | null
}

export type LocationDetailApiHeader = {
  id: number
  name: string
  city: string | null
  lifecycleStatus: LocationLifecycleStatus
  setupStatus: LocationSetupStatus
  managerName: string | null
  managerUserId: number | null
  address: string
  postcode: string | null
  locationPhone: string | null
  localContact: string | null
  liveQrCount: number
  guestsCapturedThisMonth: number
}

export type LocationDetailApiResponse = {
  success: boolean
  header: LocationDetailApiHeader
  setupChecklist: Record<string, LocationSetupChecklistStatusId>
  teamAccessRows: LocationDetailApiTeamAccessRow[]
}

export function mapLocationDetailTeamAccessRows(
  rows: LocationDetailApiTeamAccessRow[] | undefined,
  getNow: () => Date = () => new Date()
): LocationDetailTeamAccessRow[] {
  return (rows ?? []).map((row) => ({
    id: String(row.membershipId),
    name: row.name,
    role: row.role,
    accessLabel: row.accessLabel,
    lastActiveLabel:
      row.lastActiveAt != null
        ? formatAccessActivityOccurredAt(row.lastActiveAt, getNow())
        : "—",
  }))
}

export function mapLocationDetailSetupChecklist(
  wire: Record<string, LocationSetupChecklistStatusId>
): Record<LocationSetupChecklistItemId, LocationSetupChecklistStatusId> {
  return {
    locationDetailsAdded: wire.locationDetailsAdded ?? "not-started",
    qrCodePublishedLive: wire.qrCodePublishedLive ?? "not-started",
    guestFormConnected: wire.guestFormConnected ?? "not-started",
    teamAccessAssigned: wire.teamAccessAssigned ?? "optional",
    guestPrivacyNotice: wire.guestPrivacyNotice ?? "not-started",
    firstOfferCreated: wire.firstOfferCreated ?? "optional",
    atLeastOneQrCreated: wire.atLeastOneQrCreated ?? "not-started",
  }
}
