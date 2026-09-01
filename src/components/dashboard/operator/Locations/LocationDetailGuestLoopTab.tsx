import type { LocationDetailSnapshot } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import { LocationDetailAssignedQrSection } from "@/components/dashboard/operator/Locations/LocationDetailAssignedQrSection"
import { LocationDetailGuestActivitySection } from "@/components/dashboard/operator/Locations/LocationDetailGuestActivitySection"
import { LocationDetailOffersSection } from "@/components/dashboard/operator/Locations/LocationDetailOffersSection"

type LocationDetailGuestLoopTabProps = {
  snap: LocationDetailSnapshot
  createQrPath: string
  createOfferPath: string
  createCampaignPath: string
  guestsPath: string
  feedbackPath: string
  redemptionsPath: string
  guestProfilePathFor: (locationGuestId: number) => string
}

export function LocationDetailGuestLoopTab({
  snap,
  createQrPath,
  createOfferPath,
  createCampaignPath,
  guestsPath,
  feedbackPath,
  redemptionsPath,
  guestProfilePathFor,
}: LocationDetailGuestLoopTabProps) {
  return (
    <div className="flex flex-col gap-5">
      <LocationDetailAssignedQrSection
        qrRows={snap.qrRows}
        createQrPath={createQrPath}
      />

      <LocationDetailGuestActivitySection
        guestActivityChecklist={snap.guestActivityChecklist}
        latestFeedbackRows={snap.latestFeedbackRows}
        guestsPath={guestsPath}
        feedbackPath={feedbackPath}
        redemptionsPath={redemptionsPath}
        guestProfilePathFor={guestProfilePathFor}
      />

      <LocationDetailOffersSection
        offerCards={snap.offerCards}
        createOfferPath={createOfferPath}
        createCampaignPath={createCampaignPath}
        showCreateButtonsWhenEmpty
      />
    </div>
  )
}
