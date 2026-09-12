import { useEffect, useRef, useSyncExternalStore } from "react"

import { getGlobalSearch } from "@/api/dashboardApi"
import {
  createOperatorGlobalSearchModule,
  mapSearchHit,
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
  selectCampaignHit: (campaignId: string) => void
  selectOfferHit: (offerId: string) => void
}

export function useGlobalSearchModule(args: {
  handoffSuggestionToAssistant: OperatorGlobalSearchHandoff
  getLocationId: () => number | null
  navigateToGuestProfile: (guestId: number, locationId: number) => void
  navigateToCampaignDetail: (campaignId: number, locationId: number) => void
  navigateToOfferDetails: (offerId: number, locationId: number) => void
}): OperatorGlobalSearchApi {
  const handoffRef = useRef(args.handoffSuggestionToAssistant)
  handoffRef.current = args.handoffSuggestionToAssistant
  const getLocationIdRef = useRef(args.getLocationId)
  getLocationIdRef.current = args.getLocationId
  const navigateGuestRef = useRef(args.navigateToGuestProfile)
  navigateGuestRef.current = args.navigateToGuestProfile
  const navigateCampaignRef = useRef(args.navigateToCampaignDetail)
  navigateCampaignRef.current = args.navigateToCampaignDetail
  const navigateOfferRef = useRef(args.navigateToOfferDetails)
  navigateOfferRef.current = args.navigateToOfferDetails

  const moduleRef = useRef<OperatorGlobalSearchModule | null>(null)
  if (moduleRef.current == null) {
    moduleRef.current = createOperatorGlobalSearchModule(
      {
        handoffSuggestionToAssistant: (prompt) => {
          handoffRef.current(prompt)
        },
        getLocationId: () => getLocationIdRef.current(),
        navigateToGuestProfile: (guestId, locationId) => {
          navigateGuestRef.current(guestId, locationId)
        },
        navigateToCampaignDetail: (campaignId, locationId) => {
          navigateCampaignRef.current(campaignId, locationId)
        },
        navigateToOfferDetails: (offerId, locationId) => {
          navigateOfferRef.current(offerId, locationId)
        },
        searchHits: async ({ q, locationId, signal }) => {
          const response = await getGlobalSearch({
            q,
            locationId,
            types: "guests,campaigns,offers",
            signal,
          })
          const guestsGroup = response.groups.find(
            (group) => group.type === "guests"
          )
          const campaignsGroup = response.groups.find(
            (group) => group.type === "campaigns"
          )
          const offersGroup = response.groups.find(
            (group) => group.type === "offers"
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
    selectCampaignHit: search.selectCampaignHit,
    selectOfferHit: search.selectOfferHit,
  }
}
