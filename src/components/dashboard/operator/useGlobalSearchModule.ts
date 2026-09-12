import { useEffect, useRef, useSyncExternalStore } from "react"

import { getGlobalSearch } from "@/api/dashboardApi"
import {
  createOperatorGlobalSearchModule,
  mapSearchHit,
  type OperatorGlobalSearchLocationScope,
  type OperatorGlobalSearchModule,
  type OperatorGlobalSearchSnapshot,
} from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"

function readIsApplePlatform(): boolean {
  if (typeof navigator === "undefined") {
    return false
  }
  const platform = navigator.platform || ""
  const ua = navigator.userAgent || ""
  return /Mac|iPhone|iPad|iPod/i.test(platform) || /Mac OS|iPhone|iPad|iPod/i.test(ua)
}

export type OperatorGlobalSearchHandoff = (prompt: string) => void

export type OperatorGlobalSearchApi = {
  snapshot: OperatorGlobalSearchSnapshot
  open: () => void
  close: () => void
  setOpen: (open: boolean) => void
  setQuery: (query: string) => void
  selectSuggestion: (suggestionId: string) => void
  selectGuestHit: (guestId: string) => void
  selectFeedbackHit: (feedbackId: string) => void
  selectCampaignHit: (campaignId: string) => void
  selectOfferHit: (offerId: string) => void
  selectQrCodeHit: (qrCodeId: string) => void
  setLocationScope: (scope: OperatorGlobalSearchLocationScope) => void
  widenToAllLocations: () => void
  notifyOwnedLocationChanged: () => void
}

export function useGlobalSearchModule(args: {
  handoffSuggestionToAssistant: OperatorGlobalSearchHandoff
  getLocationId: () => number | null
  getAuthorisedLocationCount: () => number
  navigateToGuestProfile: (guestId: number, locationId: number) => void
  navigateToFeedbackDetail: (feedbackId: number, locationId: number) => void
  navigateToCampaignDetail: (campaignId: number, locationId: number) => void
  navigateToOfferDetails: (offerId: number, locationId: number) => void
  navigateToCapturePlacementDetail: (
    qrCodeId: number,
    locationId: number
  ) => void
}): OperatorGlobalSearchApi {
  const handoffRef = useRef(args.handoffSuggestionToAssistant)
  handoffRef.current = args.handoffSuggestionToAssistant
  const getLocationIdRef = useRef(args.getLocationId)
  getLocationIdRef.current = args.getLocationId
  const getAuthorisedLocationCountRef = useRef(args.getAuthorisedLocationCount)
  getAuthorisedLocationCountRef.current = args.getAuthorisedLocationCount
  const navigateGuestRef = useRef(args.navigateToGuestProfile)
  navigateGuestRef.current = args.navigateToGuestProfile
  const navigateFeedbackRef = useRef(args.navigateToFeedbackDetail)
  navigateFeedbackRef.current = args.navigateToFeedbackDetail
  const navigateCampaignRef = useRef(args.navigateToCampaignDetail)
  navigateCampaignRef.current = args.navigateToCampaignDetail
  const navigateOfferRef = useRef(args.navigateToOfferDetails)
  navigateOfferRef.current = args.navigateToOfferDetails
  const navigateCaptureRef = useRef(args.navigateToCapturePlacementDetail)
  navigateCaptureRef.current = args.navigateToCapturePlacementDetail

  const moduleRef = useRef<OperatorGlobalSearchModule | null>(null)
  if (moduleRef.current == null) {
    moduleRef.current = createOperatorGlobalSearchModule(
      {
        handoffSuggestionToAssistant: (prompt) => {
          handoffRef.current(prompt)
        },
        getLocationId: () => getLocationIdRef.current(),
        getAuthorisedLocationCount: () => getAuthorisedLocationCountRef.current(),
        navigateToGuestProfile: (guestId, locationId) => {
          navigateGuestRef.current(guestId, locationId)
        },
        navigateToFeedbackDetail: (feedbackId, locationId) => {
          navigateFeedbackRef.current(feedbackId, locationId)
        },
        navigateToCampaignDetail: (campaignId, locationId) => {
          navigateCampaignRef.current(campaignId, locationId)
        },
        navigateToOfferDetails: (offerId, locationId) => {
          navigateOfferRef.current(offerId, locationId)
        },
        navigateToCapturePlacementDetail: (qrCodeId, locationId) => {
          navigateCaptureRef.current(qrCodeId, locationId)
        },
        searchHits: async ({ q, locationId, scope, signal }) => {
          const response = await getGlobalSearch({
            q,
            locationId,
            types: "guests,feedback,campaigns,offers,qr-codes",
            scope,
            signal,
          })
          const guestsGroup = response.groups.find(
            (group) => group.type === "guests"
          )
          const feedbackGroup = response.groups.find(
            (group) => group.type === "feedback"
          )
          const campaignsGroup = response.groups.find(
            (group) => group.type === "campaigns"
          )
          const offersGroup = response.groups.find(
            (group) => group.type === "offers"
          )
          const qrGroup = response.groups.find(
            (group) => group.type === "qr-codes"
          )
          return {
            guestHits: (guestsGroup?.hits ?? []).map((hit) =>
              mapSearchHit({
                id: hit.id,
                title: hit.title,
                subtitle: hit.subtitle,
                status: hit.status,
                locationId: hit.locationId,
              })
            ),
            feedbackHits: (feedbackGroup?.hits ?? []).map((hit) =>
              mapSearchHit({
                id: hit.id,
                title: hit.title,
                subtitle: hit.subtitle,
                status: hit.status,
                locationId: hit.locationId,
              })
            ),
            campaignHits: (campaignsGroup?.hits ?? []).map((hit) =>
              mapSearchHit({
                id: hit.id,
                title: hit.title,
                subtitle: hit.subtitle,
                status: hit.status,
                locationId: hit.locationId,
              })
            ),
            offerHits: (offersGroup?.hits ?? []).map((hit) =>
              mapSearchHit({
                id: hit.id,
                title: hit.title,
                subtitle: hit.subtitle,
                status: hit.status,
                locationId: hit.locationId,
              })
            ),
            qrCodeHits: (qrGroup?.hits ?? []).map((hit) =>
              mapSearchHit({
                id: hit.id,
                title: hit.title,
                subtitle: hit.subtitle,
                status: hit.status,
                locationId: hit.locationId,
              })
            ),
          }
        },
      },
      { isApplePlatform: readIsApplePlatform }
    )
  }
  const search = moduleRef.current

  const snapshot = useSyncExternalStore(
    search.subscribe,
    search.getSnapshot,
    search.getSnapshot
  )

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const handled = search.handleShortcutKeydown({
        key: event.key,
        metaKey: event.metaKey,
        ctrlKey: event.ctrlKey,
        isApplePlatform: readIsApplePlatform(),
      })
      if (handled) {
        event.preventDefault()
      }
    }

    window.addEventListener("keydown", onKeyDown)
    return () => {
      window.removeEventListener("keydown", onKeyDown)
    }
  }, [search])

  return {
    snapshot,
    open: search.open,
    close: search.close,
    setOpen: search.setOpen,
    setQuery: search.setQuery,
    selectSuggestion: search.selectSuggestion,
    selectGuestHit: search.selectGuestHit,
    selectFeedbackHit: search.selectFeedbackHit,
    selectCampaignHit: search.selectCampaignHit,
    selectOfferHit: search.selectOfferHit,
    selectQrCodeHit: search.selectQrCodeHit,
    setLocationScope: search.setLocationScope,
    widenToAllLocations: search.widenToAllLocations,
    notifyOwnedLocationChanged: search.notifyOwnedLocationChanged,
  }
}
