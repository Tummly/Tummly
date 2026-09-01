import type { LocationLifecycleStatus } from "@/lib/operatorLocations/locationsPresentation"
import type { LocationSetupStatus } from "@/lib/operatorLocations/locationsPresentation"
import type {
  LocationSetupChecklistItemId,
  LocationSetupChecklistStatusId,
} from "@/lib/operatorLocations/locationDetailPresentation"

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
