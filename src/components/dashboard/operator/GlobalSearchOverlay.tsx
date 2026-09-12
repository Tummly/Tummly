import { ArrowRightIcon, ArrowUpIcon, ArrowDownIcon, CornerDownLeftIcon } from "lucide-react"

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
  GLOBAL_SEARCH_CAMPAIGNS_HEADING,
  GLOBAL_SEARCH_DIALOG_TITLE,
  GLOBAL_SEARCH_ENTITY_AVATAR_CLASS,
  GLOBAL_SEARCH_ENTITY_ROW_CLASS,
  GLOBAL_SEARCH_FEEDBACK_HEADING,
  GLOBAL_SEARCH_FOOTER_CLASS,
  GLOBAL_SEARCH_FOOTER_HINT_CLASS,
  GLOBAL_SEARCH_GUESTS_HEADING,
  GLOBAL_SEARCH_INPUT_CLASS,
  GLOBAL_SEARCH_INPUT_ROW_CLASS,
  GLOBAL_SEARCH_KBD_CLASS,
  GLOBAL_SEARCH_NO_RESULTS_MESSAGE,
  GLOBAL_SEARCH_OFFERS_HEADING,
  GLOBAL_SEARCH_OVERLAY_CLASS,
  GLOBAL_SEARCH_PLACEHOLDER,
  GLOBAL_SEARCH_QR_CODES_HEADING,
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
  onLocationScopeChange,
  onWidenToAllLocations,
}: GlobalSearchOverlayProps) {
  const trimmedQuery = snapshot.query.trim()
  const showEmptyAi =
    trimmedQuery.length === 0 && snapshot.emptySuggestions.length > 0
  // Entity groups after debounce starts (pending or hits), not on raw keystrokes alone.
  const showEntityGroups = trimmedQuery.length >= 2 && snapshot.hitsPending
  const showGuestsGroup =
    showEntityGroups ||
    (trimmedQuery.length >= 2 && snapshot.guestHits.length > 0)
  const showFeedbackGroup =
    showEntityGroups ||
    (trimmedQuery.length >= 2 && snapshot.feedbackHits.length > 0)
  const showCampaignsGroup =
    showEntityGroups ||
    (trimmedQuery.length >= 2 && snapshot.campaignHits.length > 0)
  const showOffersGroup =
    showEntityGroups ||
    (trimmedQuery.length >= 2 && snapshot.offerHits.length > 0)
  const showQrCodesGroup =
    showEntityGroups ||
    (trimmedQuery.length >= 2 && snapshot.qrCodeHits.length > 0)

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
            value={snapshot.query}
            onChange={(event) => onQueryChange(event.target.value)}
            placeholder={GLOBAL_SEARCH_PLACEHOLDER}
            className={GLOBAL_SEARCH_INPUT_CLASS}
            autoFocus
            aria-label={GLOBAL_SEARCH_DIALOG_TITLE}
          />
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
                {snapshot.emptySuggestions.map((suggestion) => (
                  <CommandItem
                    key={suggestion.id}
                    value={suggestion.id}
                    onSelect={() => onSelectSuggestion(suggestion.id)}
                    className={GLOBAL_SEARCH_AI_ROW_CLASS}
                  >
                    <AiIcon size={18} className="text-op-text-primary" />
                    <span className="min-w-0 flex-1 truncate">
                      {suggestion.prompt}
                    </span>
                    <ArrowRightIcon
                      className="size-3.5 shrink-0 text-op-header-search-text"
                      aria-hidden
                    />
                  </CommandItem>
                ))}
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
              </CommandGroup>
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

            {snapshot.showNoResults && !snapshot.showWidenFromNoResults ? (
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
