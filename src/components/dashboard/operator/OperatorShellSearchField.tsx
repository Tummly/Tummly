import { useEffect, useRef, useState } from "react"
import { Loader2Icon } from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import {
  GlobalSearchResultsPanel,
  type GlobalSearchInputNav,
} from "@/components/dashboard/operator/GlobalSearchResultsPanel"
import {
  OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
  OPERATOR_UTILITY_SURFACE_CLASS,
} from "@/components/dashboard/operator/ShellUtilityChrome"
import { Button } from "@/components/ui/button"
import { Kbd, KbdGroup } from "@/components/ui/kbd"
import {
  Popover,
  PopoverAnchor,
  PopoverContent,
} from "@/components/ui/popover"
import type {
  OperatorGlobalSearchLocationScope,
  OperatorGlobalSearchSnapshot,
} from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"
import {
  GLOBAL_SEARCH_DIALOG_TITLE,
  GLOBAL_SEARCH_KBD_CLASS,
  GLOBAL_SEARCH_PLACEHOLDER,
  GLOBAL_SEARCH_POPOVER_CLASS,
  GLOBAL_SEARCH_TRIGGER_CLASS,
  GLOBAL_SEARCH_TRIGGER_PLACEHOLDER,
} from "@/lib/operatorGlobalSearch/globalSearchPresentation"
import { cn } from "@/lib/utils"

type GlobalSearchResultsHandlers = {
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

type OperatorShellSearchFieldProps = {
  className?: string
  /** Tighter sizing for mobile navbar sheet header. */
  compact?: boolean
  shortcutModifierLabel: string
  /**
   * Mobile nav sheet: button that opens the full-screen Dialog.
   * Desktop navbar: omit — field is a live input with Popover results.
   */
  onOpen?: () => void
  /** Desktop live Search (navbar). */
  snapshot?: OperatorGlobalSearchSnapshot
  onOpenChange?: (open: boolean) => void
  onQueryChange?: (query: string) => void
  resultsHandlers?: GlobalSearchResultsHandlers
}

/** Live Global Search trigger — navbar (input + Popover) and mobile nav sheet (button). */
export function OperatorShellSearchField({
  className,
  compact = false,
  shortcutModifierLabel,
  onOpen,
  snapshot,
  onOpenChange,
  onQueryChange,
  resultsHandlers,
}: OperatorShellSearchFieldProps) {
  const isDesktopLive =
    snapshot != null &&
    onOpenChange != null &&
    onQueryChange != null &&
    resultsHandlers != null

  if (!isDesktopLive) {
    return (
      <Button
        type="button"
        variant="ghost"
        role="search"
        aria-label="Search"
        title="Open Global Search"
        className={cn(
          GLOBAL_SEARCH_TRIGGER_CLASS,
          compact
            ? OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS
            : cn(
                OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
                "lg:h-10 lg:min-h-10"
              ),
          OPERATOR_UTILITY_SURFACE_CLASS,
          "justify-start hover:bg-op-header-search-hover",
          className
        )}
        onClick={onOpen}
      >
        <OperatorSearchIcon className="size-3.5 shrink-0 text-op-header-search-text lg:size-4" />
        <span className="min-w-0 flex-1 truncate">
          {GLOBAL_SEARCH_TRIGGER_PLACEHOLDER}
        </span>
        <KbdGroup className="hidden shrink-0 gap-1 lg:inline-flex">
          <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>{shortcutModifierLabel}</Kbd>
          <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>K</Kbd>
        </KbdGroup>
      </Button>
    )
  }

  return (
    <DesktopSearchField
      className={className}
      compact={compact}
      shortcutModifierLabel={shortcutModifierLabel}
      snapshot={snapshot}
      onOpenChange={onOpenChange}
      onQueryChange={onQueryChange}
      resultsHandlers={resultsHandlers}
    />
  )
}

function useIsLgUp(): boolean {
  const [isLgUp, setIsLgUp] = useState(() =>
    typeof window !== "undefined"
      ? window.matchMedia("(min-width: 1024px)").matches
      : true
  )

  useEffect(() => {
    const media = window.matchMedia("(min-width: 1024px)")
    const sync = () => setIsLgUp(media.matches)
    sync()
    media.addEventListener("change", sync)
    return () => media.removeEventListener("change", sync)
  }, [])

  return isLgUp
}

function DesktopSearchField({
  className,
  compact,
  shortcutModifierLabel,
  snapshot,
  onOpenChange,
  onQueryChange,
  resultsHandlers,
}: {
  className?: string
  compact: boolean
  shortcutModifierLabel: string
  snapshot: OperatorGlobalSearchSnapshot
  onOpenChange: (open: boolean) => void
  onQueryChange: (query: string) => void
  resultsHandlers: GlobalSearchResultsHandlers
}) {
  const inputRef = useRef<HTMLInputElement>(null)
  const fieldRef = useRef<HTMLDivElement>(null)
  const inputNavRef = useRef<GlobalSearchInputNav | null>(null)
  const [fieldWidthPx, setFieldWidthPx] = useState<number | null>(null)
  const isLgUp = useIsLgUp()
  const open = snapshot.open && isLgUp

  useEffect(() => {
    if (!open) {
      return
    }
    const id = window.requestAnimationFrame(() => {
      inputRef.current?.focus()
    })
    return () => window.cancelAnimationFrame(id)
  }, [open])

  useEffect(() => {
    const field = fieldRef.current
    if (field == null) {
      return
    }
    const syncWidth = () => {
      setFieldWidthPx(field.getBoundingClientRect().width)
    }
    syncWidth()
    const observer = new ResizeObserver(syncWidth)
    observer.observe(field)
    return () => observer.disconnect()
  }, [])

  const fieldChrome = cn(
    GLOBAL_SEARCH_TRIGGER_CLASS,
    compact
      ? OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS
      : cn(
          OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
          "lg:h-10 lg:min-h-10"
        ),
    OPERATOR_UTILITY_SURFACE_CLASS,
    "justify-start",
    open && "rounded-b-none ring-1 ring-op-card-border",
    className
  )

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (!isLgUp) {
          return
        }
        onOpenChange(next)
      }}
      modal={false}
    >
      <PopoverAnchor asChild>
        <div ref={fieldRef} role="search" className={fieldChrome}>
          <OperatorSearchIcon className="size-3.5 shrink-0 text-op-header-search-text lg:size-4" />
          <input
            ref={inputRef}
            type="search"
            role="searchbox"
            value={snapshot.query}
            onChange={(event) => onQueryChange(event.target.value)}
            onFocus={() => {
              if (!snapshot.open) {
                onOpenChange(true)
              }
            }}
            onKeyDown={(event) => {
              if (event.key === "Escape") {
                event.preventDefault()
                onOpenChange(false)
                return
              }
              if (inputNavRef.current?.handleKeyDown(event)) {
                event.preventDefault()
              }
            }}
            placeholder={
              open ? GLOBAL_SEARCH_PLACEHOLDER : GLOBAL_SEARCH_TRIGGER_PLACEHOLDER
            }
            className="min-w-0 flex-1 border-0 bg-transparent text-inherit outline-none placeholder:text-op-header-search-text focus-visible:ring-0"
            aria-label={GLOBAL_SEARCH_DIALOG_TITLE}
            aria-expanded={open}
            aria-controls={open ? "global-search-results" : undefined}
            autoComplete="off"
          />
          {snapshot.hitsPending ? (
            <Loader2Icon
              className="size-4 shrink-0 animate-spin text-op-header-search-text"
              aria-hidden
            />
          ) : null}
          {!open ? (
            <KbdGroup className="hidden shrink-0 gap-1 lg:inline-flex">
              <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>
                {shortcutModifierLabel}
              </Kbd>
              <Kbd className={GLOBAL_SEARCH_KBD_CLASS}>K</Kbd>
            </KbdGroup>
          ) : null}
        </div>
      </PopoverAnchor>
      <PopoverContent
        id="global-search-results"
        align="start"
        side="bottom"
        sideOffset={0}
        onOpenAutoFocus={(event) => event.preventDefault()}
        onCloseAutoFocus={(event) => event.preventDefault()}
        onInteractOutside={(event) => {
          const target = event.target as Node | null
          if (target != null && fieldRef.current?.contains(target)) {
            event.preventDefault()
          }
        }}
        className={GLOBAL_SEARCH_POPOVER_CLASS}
        style={
          fieldWidthPx != null ? { width: fieldWidthPx } : undefined
        }
      >
        <GlobalSearchResultsPanel
          snapshot={snapshot}
          inputNavRef={inputNavRef}
          {...resultsHandlers}
        />
      </PopoverContent>
    </Popover>
  )
}
