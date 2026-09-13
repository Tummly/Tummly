import { ArrowRightIcon, ArrowUpIcon, ArrowDownIcon, CornerDownLeftIcon, Loader2Icon } from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { AiIcon } from "@/components/ui/ai-icon"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Command,
  CommandGroup,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog"
import { Kbd } from "@/components/ui/kbd"
import type {
  OperatorGlobalSearchEntityHit,
  OperatorGlobalSearchLocationScope,
  OperatorGlobalSearchSnapshot,
} from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"
import {
  GLOBAL_SEARCH_AI_HEADING,
  GLOBAL_SEARCH_AI_ROW_CLASS,
  GLOBAL_SEARCH_ALL_LOCATIONS_LABEL,
  GLOBAL_SEARCH_ASK_TUMMLY_HEADING,
  GLOBAL_SEARCH_CAMPAIGNS_HEADING,
  GLOBAL_SEARCH_DIALOG_TITLE,
  GLOBAL_SEARCH_ENTITY_AVATAR_CLASS,
  GLOBAL_SEARCH_ENTITY_ROW_CLASS,
  GLOBAL_SEARCH_ERROR_MESSAGE,
  GLOBAL_SEARCH_FEEDBACK_HEADING,
  GLOBAL_SEARCH_FOOTER_CLASS,
  GLOBAL_SEARCH_FOOTER_HINT_CLASS,
  GLOBAL_SEARCH_GUESTS_HEADING,
  GLOBAL_SEARCH_INPUT_CLASS,
  GLOBAL_SEARCH_INPUT_ROW_CLASS,
  GLOBAL_SEARCH_KBD_CLASS,
  GLOBAL_SEARCH_NO_RESULTS_MESSAGE,
  GLOBAL_SEARCH_OFFERS_HEADING,
  GLOBAL_SEARCH_OFFLINE_MESSAGE,
  GLOBAL_SEARCH_OVERLAY_CLASS,
  GLOBAL_SEARCH_PARTIAL_WARNING,
  GLOBAL_SEARCH_PLACEHOLDER,
  GLOBAL_SEARCH_QR_CODES_HEADING,
  GLOBAL_SEARCH_TRY_AGAIN_LABEL,
  GLOBAL_SEARCH_VIEW_ALL_CAMPAIGNS,
  GLOBAL_SEARCH_VIEW_ALL_FEEDBACK,
  GLOBAL_SEARCH_VIEW_ALL_GUESTS,
  GLOBAL_SEARCH_VIEW_ALL_OFFERS,
  GLOBAL_SEARCH_VIEW_ALL_QR_CODES,
  GLOBAL_SEARCH_WIDEN_FROM_NO_RESULTS_LABEL,
} from "@/lib/operatorGlobalSearch/globalSearchPresentation"
import { cn } from "@/lib/utils"

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

const COMMAND_GROUP_HEADING_CLASS = cn(
  "p-0",
  "**:[[cmdk-group-heading]]:px-5 **:[[cmdk-group-heading]]:pt-5 **:[[cmdk-group-heading]]:pb-3",
  "**:[[cmdk-group-heading]]:text-sm **:[[cmdk-group-heading]]:font-medium",
  "**:[[cmdk-group-heading]]:text-op-header-search-text"
)

function EntityHitRows({
  hits,
  valuePrefix,
  onSelect,
}: {
  hits: readonly OperatorGlobalSearchEntityHit[]
  valuePrefix: string
  onSelect: (id: string) => void
}) {
  return hits.map((hit) => (
    <CommandItem
      key={`${valuePrefix}-${hit.id}`}
      value={`${valuePrefix}-${hit.id}`}
      onSelect={() => onSelect(hit.id)}
      className={GLOBAL_SEARCH_ENTITY_ROW_CLASS}
    >
      <Avatar className="size-8 shrink-0">
        <AvatarFallback className={GLOBAL_SEARCH_ENTITY_AVATAR_CLASS}>
          {hit.initials}
        </AvatarFallback>
      </Avatar>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-op-text-primary">{hit.title}</span>
        {hit.subtitle ? (
          <span className="block truncate text-xs text-op-header-search-text">
            {hit.subtitle}
          </span>
        ) : null}
      </span>
      {hit.status ? (
        <Badge variant="soft" className="shrink-0">
          {hit.status}
        </Badge>
      ) : null}
    </CommandItem>
  ))
}

function SuggestionRows({
  suggestions,
  onSelect,
}: {
  suggestions: OperatorGlobalSearchSnapshot["emptySuggestions"]
  onSelect: (id: string) => void
}) {
  return suggestions.map((suggestion) => (
    <CommandItem
      key={suggestion.id}
      value={suggestion.id}
      onSelect={() => onSelect(suggestion.id)}
      className={GLOBAL_SEARCH_AI_ROW_CLASS}
    >
      <AiIcon size={18} className="text-op-text-primary" />
      <span className="min-w-0 flex-1 truncate">{suggestion.prompt}</span>
      <ArrowRightIcon
        className="size-3.5 shrink-0 text-op-header-search-text"
        aria-hidden
      />
    </CommandItem>
  ))
}

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
  const trimmedQuery = snapshot.query.trim()
  const showEmptyAi =
    trimmedQuery.length === 0 && snapshot.emptySuggestions.length > 0
  const showTypedAskTummly =
    trimmedQuery.length >= 2 && snapshot.typedSuggestions.length > 0
  // Keep entity groups visible while pending so the panel does not flicker empty.
  const showGuestsGroup =
    trimmedQuery.length >= 2 && snapshot.guestHits.length > 0
  const showFeedbackGroup =
    trimmedQuery.length >= 2 && snapshot.feedbackHits.length > 0
  const showCampaignsGroup =
    trimmedQuery.length >= 2 && snapshot.campaignHits.length > 0
  const showOffersGroup =
    trimmedQuery.length >= 2 && snapshot.offerHits.length > 0
  const showQrCodesGroup =
    trimmedQuery.length >= 2 && snapshot.qrCodeHits.length > 0

  return (
    <Dialog open={snapshot.open} onOpenChange={onOpenChange}>
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

        <div
          className="sr-only"
          aria-live="polite"
          aria-atomic="true"
        >
          {snapshot.resultCountAnnouncement}
        </div>

        <div className={GLOBAL_SEARCH_INPUT_ROW_CLASS}>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="shrink-0 text-op-text-primary md:hidden"
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
            placeholder={GLOBAL_SEARCH_PLACEHOLDER}
            className={GLOBAL_SEARCH_INPUT_CLASS}
            autoFocus
            aria-label={GLOBAL_SEARCH_DIALOG_TITLE}
          />
          {snapshot.hitsPending ? (
            <Loader2Icon
              className="size-4 shrink-0 animate-spin text-op-header-search-text"
              aria-hidden
            />
          ) : null}
        </div>

        {snapshot.canWidenLocationScope ? (
          <div className="border-b border-op-card-border px-5 py-3">
            <CheckboxLabel
              checked={snapshot.locationScope === "all"}
              onCheckedChange={(checked) => {
                onLocationScopeChange(checked ? "all" : "current")
              }}
              className="gap-0"
              labelClassName="text-sm font-medium text-op-text-primary"
            >
              {GLOBAL_SEARCH_ALL_LOCATIONS_LABEL}
            </CheckboxLabel>
          </div>
        ) : null}

        {snapshot.showPartialWarning ? (
          <div className="border-b border-op-card-border px-5 py-3 text-sm text-op-header-search-text">
            {GLOBAL_SEARCH_PARTIAL_WARNING}
          </div>
        ) : null}

        <Command
          shouldFilter={false}
          className="min-h-0 flex-1 rounded-none bg-transparent p-0"
        >
          <CommandList className="max-h-none flex-1 overflow-y-auto">
            {showEmptyAi ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_AI_HEADING}
                className={COMMAND_GROUP_HEADING_CLASS}
              >
                <SuggestionRows
                  suggestions={snapshot.emptySuggestions}
                  onSelect={onSelectSuggestion}
                />
              </CommandGroup>
            ) : null}

            {showGuestsGroup ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_GUESTS_HEADING}
                className={COMMAND_GROUP_HEADING_CLASS}
              >
                <EntityHitRows
                  hits={snapshot.guestHits}
                  valuePrefix="guest"
                  onSelect={onSelectGuestHit}
                />
                {snapshot.showViewAllGuests ? (
                  <CommandItem
                    value="view-all-guests"
                    onSelect={onViewAllGuests}
                    className={GLOBAL_SEARCH_AI_ROW_CLASS}
                  >
                    <span className="min-w-0 flex-1 truncate">
                      {GLOBAL_SEARCH_VIEW_ALL_GUESTS}
                    </span>
                    <ArrowRightIcon
                      className="size-3.5 shrink-0 text-op-header-search-text"
                      aria-hidden
                    />
                  </CommandItem>
                ) : null}
              </CommandGroup>
            ) : null}

            {showFeedbackGroup ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_FEEDBACK_HEADING}
                className={COMMAND_GROUP_HEADING_CLASS}
              >
                <EntityHitRows
                  hits={snapshot.feedbackHits}
                  valuePrefix="feedback"
                  onSelect={onSelectFeedbackHit}
                />
                {snapshot.showViewAllFeedback ? (
                  <CommandItem
                    value="view-all-feedback"
                    onSelect={onViewAllFeedback}
                    className={GLOBAL_SEARCH_AI_ROW_CLASS}
                  >
                    <span className="min-w-0 flex-1 truncate">
                      {GLOBAL_SEARCH_VIEW_ALL_FEEDBACK}
                    </span>
                    <ArrowRightIcon
                      className="size-3.5 shrink-0 text-op-header-search-text"
                      aria-hidden
                    />
                  </CommandItem>
                ) : null}
              </CommandGroup>
            ) : null}

            {showCampaignsGroup ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_CAMPAIGNS_HEADING}
                className={COMMAND_GROUP_HEADING_CLASS}
              >
                <EntityHitRows
                  hits={snapshot.campaignHits}
                  valuePrefix="campaign"
                  onSelect={onSelectCampaignHit}
                />
                {snapshot.showViewAllCampaigns ? (
                  <CommandItem
                    value="view-all-campaigns"
                    onSelect={onViewAllCampaigns}
                    className={GLOBAL_SEARCH_AI_ROW_CLASS}
                  >
                    <span className="min-w-0 flex-1 truncate">
                      {GLOBAL_SEARCH_VIEW_ALL_CAMPAIGNS}
                    </span>
                    <ArrowRightIcon
                      className="size-3.5 shrink-0 text-op-header-search-text"
                      aria-hidden
                    />
                  </CommandItem>
                ) : null}
              </CommandGroup>
            ) : null}

            {showOffersGroup ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_OFFERS_HEADING}
                className={COMMAND_GROUP_HEADING_CLASS}
              >
                <EntityHitRows
                  hits={snapshot.offerHits}
                  valuePrefix="offer"
                  onSelect={onSelectOfferHit}
                />
                {snapshot.showViewAllOffers ? (
                  <CommandItem
                    value="view-all-offers"
                    onSelect={onViewAllOffers}
                    className={GLOBAL_SEARCH_AI_ROW_CLASS}
                  >
                    <span className="min-w-0 flex-1 truncate">
                      {GLOBAL_SEARCH_VIEW_ALL_OFFERS}
                    </span>
                    <ArrowRightIcon
                      className="size-3.5 shrink-0 text-op-header-search-text"
                      aria-hidden
                    />
                  </CommandItem>
                ) : null}
              </CommandGroup>
            ) : null}

            {showQrCodesGroup ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_QR_CODES_HEADING}
                className={COMMAND_GROUP_HEADING_CLASS}
              >
                <EntityHitRows
                  hits={snapshot.qrCodeHits}
                  valuePrefix="qr-code"
                  onSelect={onSelectQrCodeHit}
                />
                {snapshot.showViewAllQrCodes ? (
                  <CommandItem
                    value="view-all-qr-codes"
                    onSelect={onViewAllQrCodes}
                    className={GLOBAL_SEARCH_AI_ROW_CLASS}
                  >
                    <span className="min-w-0 flex-1 truncate">
                      {GLOBAL_SEARCH_VIEW_ALL_QR_CODES}
                    </span>
                    <ArrowRightIcon
                      className="size-3.5 shrink-0 text-op-header-search-text"
                      aria-hidden
                    />
                  </CommandItem>
                ) : null}
              </CommandGroup>
            ) : null}

            {/* Ask Tummly stays below product hits when any hits exist. */}
            {showTypedAskTummly ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_ASK_TUMMLY_HEADING}
                className={COMMAND_GROUP_HEADING_CLASS}
              >
                <SuggestionRows
                  suggestions={snapshot.typedSuggestions}
                  onSelect={onSelectSuggestion}
                />
              </CommandGroup>
            ) : null}

            {snapshot.showOffline ? (
              <div className="px-5 py-5 text-sm text-op-header-search-text">
                {GLOBAL_SEARCH_OFFLINE_MESSAGE}
              </div>
            ) : null}

            {snapshot.showError ? (
              <div className="flex flex-col items-start gap-3 px-5 py-5">
                <p className="text-sm text-op-header-search-text">
                  {GLOBAL_SEARCH_ERROR_MESSAGE}
                </p>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={onRetrySearch}
                >
                  {GLOBAL_SEARCH_TRY_AGAIN_LABEL}
                </Button>
              </div>
            ) : null}

            {snapshot.showWidenFromNoResults ? (
              <div className="flex flex-col items-start gap-3 px-5 py-5">
                <p className="text-sm text-op-header-search-text">
                  {GLOBAL_SEARCH_NO_RESULTS_MESSAGE}
                </p>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={onWidenToAllLocations}
                >
                  {GLOBAL_SEARCH_WIDEN_FROM_NO_RESULTS_LABEL}
                </Button>
              </div>
            ) : null}

            {snapshot.showNoResults &&
            !snapshot.showWidenFromNoResults &&
            !snapshot.showError &&
            !snapshot.showOffline ? (
              <div className="px-5 py-5 text-sm text-op-header-search-text">
                {GLOBAL_SEARCH_NO_RESULTS_MESSAGE}
              </div>
            ) : null}
          </CommandList>
        </Command>

        <div className={GLOBAL_SEARCH_FOOTER_CLASS}>
          <div className="flex items-center gap-6">
            <span className={GLOBAL_SEARCH_FOOTER_HINT_CLASS}>
              <span className="inline-flex items-center gap-1.5">
                <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>
                  <ArrowUpIcon className="size-3.5" aria-hidden />
                </Kbd>
                <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>
                  <ArrowDownIcon className="size-3.5" aria-hidden />
                </Kbd>
              </span>
              Move
            </span>
            <span className={GLOBAL_SEARCH_FOOTER_HINT_CLASS}>
              <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>
                <CornerDownLeftIcon className="size-3.5" aria-hidden />
              </Kbd>
              Open
            </span>
          </div>
          <span className={GLOBAL_SEARCH_FOOTER_HINT_CLASS}>
            <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>esc</Kbd>
            Close
          </span>
        </div>
      </DialogContent>
    </Dialog>
  )
}
