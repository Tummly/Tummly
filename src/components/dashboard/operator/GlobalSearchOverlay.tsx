import { useEffect, useRef, useState } from "react"
import { ArrowRightIcon, XIcon } from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import {
  GlobalSearchResultsPanel,
  type GlobalSearchInputNav,
} from "@/components/dashboard/operator/GlobalSearchResultsPanel"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog"
import type {
  OperatorGlobalSearchLocationScope,
  OperatorGlobalSearchSnapshot,
} from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"
import {
  GLOBAL_SEARCH_CLEAR_LABEL,
  GLOBAL_SEARCH_DIALOG_TITLE,
  GLOBAL_SEARCH_INPUT_CLASS,
  GLOBAL_SEARCH_INPUT_ROW_CLASS,
  GLOBAL_SEARCH_OVERLAY_CLASS,
  GLOBAL_SEARCH_PLACEHOLDER,
} from "@/lib/operatorGlobalSearch/globalSearchPresentation"

type GlobalSearchOverlayProps = {
  snapshot: OperatorGlobalSearchSnapshot
  onOpenChange: (open: boolean) => void
  onQueryChange: (query: string) => void
  onSelectSuggestion: (suggestionId: string) => void
  onSelectGuestHit: (guestId: string) => void
  onSelectFeedbackHit: (feedbackId: string) => void
  onSelectCampaignHit: (campaignId: string) => void
  onSelectOfferHit: (offerId: string) => void
  onSelectQrCodeHit: (qrCodeId: string) => void
  onViewAllGuests: () => void
  onViewAllFeedback: () => void
  onViewAllCampaigns: () => void
  onViewAllOffers: () => void
  onViewAllQrCodes: () => void
  onRetrySearch: () => void
  onLocationScopeChange: (scope: OperatorGlobalSearchLocationScope) => void
  onWidenToAllLocations: () => void
}

function useIsBelowLg(): boolean {
  const [belowLg, setBelowLg] = useState(false)

  useEffect(() => {
    const media = window.matchMedia("(max-width: 1023px)")
    const sync = () => setBelowLg(media.matches)
    sync()
    media.addEventListener("change", sync)
    return () => media.removeEventListener("change", sync)
  }, [])

  return belowLg
}

/**
 * Mobile / below-lg full-screen Search surface.
 * Desktop uses the navbar field + Popover (see OperatorShellSearchField).
 */
export function GlobalSearchOverlay({
  snapshot,
  onOpenChange,
  onQueryChange,
  onSelectSuggestion,
  onSelectGuestHit,
  onSelectFeedbackHit,
  onSelectCampaignHit,
  onSelectOfferHit,
  onSelectQrCodeHit,
  onViewAllGuests,
  onViewAllFeedback,
  onViewAllCampaigns,
  onViewAllOffers,
  onViewAllQrCodes,
  onRetrySearch,
  onLocationScopeChange,
  onWidenToAllLocations,
}: GlobalSearchOverlayProps) {
  const isBelowLg = useIsBelowLg()
  const open = snapshot.open && isBelowLg
  const inputNavRef = useRef<GlobalSearchInputNav | null>(null)

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!isBelowLg) {
          return
        }
        onOpenChange(next)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className={GLOBAL_SEARCH_OVERLAY_CLASS}
        aria-describedby={undefined}
      >
        <DialogTitle className="sr-only">{GLOBAL_SEARCH_DIALOG_TITLE}</DialogTitle>
        <DialogDescription className="sr-only">
          Search Guests, Feedback, Campaigns, Offers, and QR codes. Empty state
          shows AI suggestions that open the AI Assistant.
        </DialogDescription>

        <div className={GLOBAL_SEARCH_INPUT_ROW_CLASS}>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="shrink-0 text-op-text-primary"
            aria-label="Back"
            onClick={() => onOpenChange(false)}
          >
            <ArrowRightIcon className="size-4 rotate-180" aria-hidden />
          </Button>
          <OperatorSearchIcon className="size-4 shrink-0 text-op-header-search-text" />
          <input
            type="search"
            role="searchbox"
            value={snapshot.query}
            onChange={(event) => onQueryChange(event.target.value)}
            onKeyDown={(event) => {
              if (inputNavRef.current?.handleKeyDown(event)) {
                event.preventDefault()
              }
            }}
            placeholder={GLOBAL_SEARCH_PLACEHOLDER}
            className={GLOBAL_SEARCH_INPUT_CLASS}
            autoFocus
            aria-label={GLOBAL_SEARCH_DIALOG_TITLE}
          />
          {snapshot.query.length > 0 ? (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className="shrink-0 text-op-header-search-text hover:bg-transparent hover:text-op-text-primary"
              aria-label={GLOBAL_SEARCH_CLEAR_LABEL}
              onClick={() => onQueryChange("")}
            >
              <XIcon className="size-4" aria-hidden />
            </Button>
          ) : null}
        </div>

        <GlobalSearchResultsPanel
          snapshot={snapshot}
          inputNavRef={inputNavRef}
          onSelectSuggestion={onSelectSuggestion}
          onSelectGuestHit={onSelectGuestHit}
          onSelectFeedbackHit={onSelectFeedbackHit}
          onSelectCampaignHit={onSelectCampaignHit}
          onSelectOfferHit={onSelectOfferHit}
          onSelectQrCodeHit={onSelectQrCodeHit}
          onViewAllGuests={onViewAllGuests}
          onViewAllFeedback={onViewAllFeedback}
          onViewAllCampaigns={onViewAllCampaigns}
          onViewAllOffers={onViewAllOffers}
          onViewAllQrCodes={onViewAllQrCodes}
          onRetrySearch={onRetrySearch}
          onLocationScopeChange={onLocationScopeChange}
          onWidenToAllLocations={onWidenToAllLocations}
        />
      </DialogContent>
    </Dialog>
  )
}
