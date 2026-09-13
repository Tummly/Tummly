import { ArrowRightIcon } from "lucide-react"
import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type RefObject,
} from "react"

import {
  GlobalSearchKbdArrowIcon,
  GlobalSearchKbdEnterIcon,
} from "@/components/dashboard/operator/GlobalSearchKbdIcons"
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
import { Kbd } from "@/components/ui/kbd"
import {
  listGlobalSearchSelectables,
  moveGlobalSearchSelection,
} from "@/lib/operatorGlobalSearch/globalSearchCommandNav"
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
  GLOBAL_SEARCH_ENTITY_AVATAR_CLASS,
  GLOBAL_SEARCH_ENTITY_ROW_CLASS,
  GLOBAL_SEARCH_ERROR_MESSAGE,
  GLOBAL_SEARCH_FEEDBACK_HEADING,
  GLOBAL_SEARCH_FOOTER_CLASS,
  GLOBAL_SEARCH_FOOTER_HINT_CLASS,
  GLOBAL_SEARCH_GUESTS_HEADING,
  GLOBAL_SEARCH_KBD_CLASS,
  GLOBAL_SEARCH_NO_RESULTS_MESSAGE,
  GLOBAL_SEARCH_OFFERS_HEADING,
  GLOBAL_SEARCH_OFFLINE_MESSAGE,
  GLOBAL_SEARCH_PARTIAL_WARNING,
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

export type GlobalSearchInputNav = {
  /** Returns true when the key was handled (caller should preventDefault). */
  handleKeyDown: (event: { key: string }) => boolean
}

export type GlobalSearchResultsPanelProps = {
  snapshot: OperatorGlobalSearchSnapshot
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
  /** Optional class for the root (Popover vs Dialog body). */
  className?: string
  /**
   * Parent Search input calls `handleKeyDown` for Arrow/Enter so highlight
   * moves while focus stays in the field.
   */
  inputNavRef?: RefObject<GlobalSearchInputNav | null>
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

function ViewAllRow({
  value,
  label,
  onSelect,
}: {
  value: string
  label: string
  onSelect: () => void
}) {
  return (
    <CommandItem
      value={value}
      onSelect={onSelect}
      className={GLOBAL_SEARCH_AI_ROW_CLASS}
    >
      <span className="min-w-0 flex-1 truncate">{label}</span>
      <ArrowRightIcon
        className="size-3.5 shrink-0 text-op-header-search-text"
        aria-hidden
      />
    </CommandItem>
  )
}

/** Results + footer chrome shared by desktop Popover and mobile Dialog. */
export function GlobalSearchResultsPanel({
  snapshot,
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
  className,
  inputNavRef,
}: GlobalSearchResultsPanelProps) {
  const listRef = useRef<HTMLDivElement>(null)
  const [commandValue, setCommandValue] = useState("")

  const selectables = useMemo(
    () =>
      listGlobalSearchSelectables(snapshot, {
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
      }),
    [
      snapshot,
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
    ]
  )
  const selectableValues = useMemo(
    () => selectables.map((item) => item.value),
    [selectables]
  )
  const selectablesRef = useRef(selectables)
  selectablesRef.current = selectables
  const commandValueRef = useRef(commandValue)
  commandValueRef.current = commandValue

  useEffect(() => {
    if (selectableValues.length === 0) {
      setCommandValue("")
      return
    }
    if (!selectableValues.includes(commandValue)) {
      setCommandValue(selectableValues[0]!)
    }
  }, [selectableValues, commandValue])

  useEffect(() => {
    if (commandValue === "" || listRef.current == null) {
      return
    }
    const selected = listRef.current.querySelector<HTMLElement>(
      '[data-slot="command-item"][data-selected="true"]'
    )
    selected?.scrollIntoView({ block: "nearest" })
  }, [commandValue])

  useEffect(() => {
    if (inputNavRef == null) {
      return
    }
    inputNavRef.current = {
      handleKeyDown: (event) => {
        const items = selectablesRef.current
        const values = items.map((item) => item.value)
        if (values.length === 0) {
          return false
        }
        if (event.key === "ArrowDown") {
          setCommandValue((current) =>
            moveGlobalSearchSelection(values, current, 1)
          )
          return true
        }
        if (event.key === "ArrowUp") {
          setCommandValue((current) =>
            moveGlobalSearchSelection(values, current, -1)
          )
          return true
        }
        if (event.key === "Home") {
          setCommandValue(values[0]!)
          return true
        }
        if (event.key === "End") {
          setCommandValue(values[values.length - 1]!)
          return true
        }
        if (event.key === "Enter") {
          const current = commandValueRef.current
          const match =
            items.find((item) => item.value === current) ?? items[0]
          match?.activate()
          return true
        }
        return false
      },
    }
    return () => {
      inputNavRef.current = null
    }
  }, [inputNavRef])

  const trimmedQuery = snapshot.query.trim()
  const showEmptyAi =
    trimmedQuery.length === 0 && snapshot.emptySuggestions.length > 0
  const showTypedAskTummly =
    trimmedQuery.length >= 2 && snapshot.typedSuggestions.length > 0
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
    <div
      ref={listRef}
      className={cn("flex min-h-0 flex-1 flex-col", className)}
    >
      <div
        className="sr-only"
        aria-live="polite"
        aria-atomic="true"
      >
        {snapshot.resultCountAnnouncement}
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
        value={commandValue}
        onValueChange={setCommandValue}
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
                <ViewAllRow
                  value="view-all-guests"
                  label={GLOBAL_SEARCH_VIEW_ALL_GUESTS}
                  onSelect={onViewAllGuests}
                />
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
                <ViewAllRow
                  value="view-all-feedback"
                  label={GLOBAL_SEARCH_VIEW_ALL_FEEDBACK}
                  onSelect={onViewAllFeedback}
                />
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
                <ViewAllRow
                  value="view-all-campaigns"
                  label={GLOBAL_SEARCH_VIEW_ALL_CAMPAIGNS}
                  onSelect={onViewAllCampaigns}
                />
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
                <ViewAllRow
                  value="view-all-offers"
                  label={GLOBAL_SEARCH_VIEW_ALL_OFFERS}
                  onSelect={onViewAllOffers}
                />
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
                <ViewAllRow
                  value="view-all-qr-codes"
                  label={GLOBAL_SEARCH_VIEW_ALL_QR_CODES}
                  onSelect={onViewAllQrCodes}
                />
              ) : null}
            </CommandGroup>
          ) : null}

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
              <Kbd className={cn(GLOBAL_SEARCH_KBD_CLASS, "size-[18px] min-h-[26px] min-w-[18px] p-1")}>
                <GlobalSearchKbdArrowIcon className="-scale-y-100" />
              </Kbd>
              <Kbd className={cn(GLOBAL_SEARCH_KBD_CLASS, "size-[18px] min-h-[26px] min-w-[18px] p-1")}>
                <GlobalSearchKbdArrowIcon />
              </Kbd>
            </span>
            Move
          </span>
          <span className={GLOBAL_SEARCH_FOOTER_HINT_CLASS}>
            <Kbd className={cn(GLOBAL_SEARCH_KBD_CLASS, "size-[18px] min-h-[26px] min-w-[18px] p-1")}>
              <GlobalSearchKbdEnterIcon />
            </Kbd>
            Open
          </span>
        </div>
        <span className={GLOBAL_SEARCH_FOOTER_HINT_CLASS}>
          <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>esc</Kbd>
          Close
        </span>
      </div>
    </div>
  )
}
