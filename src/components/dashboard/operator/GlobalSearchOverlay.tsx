import { ArrowRightIcon, ArrowUpIcon, ArrowDownIcon, CornerDownLeftIcon } from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
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
import type { OperatorGlobalSearchSnapshot } from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"
import {
  GLOBAL_SEARCH_AI_HEADING,
  GLOBAL_SEARCH_AI_ROW_CLASS,
  GLOBAL_SEARCH_DIALOG_TITLE,
  GLOBAL_SEARCH_FOOTER_CLASS,
  GLOBAL_SEARCH_FOOTER_HINT_CLASS,
  GLOBAL_SEARCH_INPUT_CLASS,
  GLOBAL_SEARCH_INPUT_ROW_CLASS,
  GLOBAL_SEARCH_KBD_CLASS,
  GLOBAL_SEARCH_OVERLAY_CLASS,
  GLOBAL_SEARCH_PLACEHOLDER,
} from "@/lib/operatorGlobalSearch/globalSearchPresentation"
import { cn } from "@/lib/utils"

type GlobalSearchOverlayProps = {
  snapshot: OperatorGlobalSearchSnapshot
  onOpenChange: (open: boolean) => void
  onQueryChange: (query: string) => void
  onSelectSuggestion: (suggestionId: string) => void
}

export function GlobalSearchOverlay({
  snapshot,
  onOpenChange,
  onQueryChange,
  onSelectSuggestion,
}: GlobalSearchOverlayProps) {
  const showEmptyAi =
    snapshot.query.trim().length === 0 && snapshot.emptySuggestions.length > 0

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

        <Command
          shouldFilter={false}
          className="min-h-0 flex-1 rounded-none bg-transparent p-0"
        >
          <CommandList className="max-h-none flex-1 overflow-y-auto">
            {showEmptyAi ? (
              <CommandGroup
                heading={GLOBAL_SEARCH_AI_HEADING}
                className={cn(
                  "p-0",
                  "**:[[cmdk-group-heading]]:px-5 **:[[cmdk-group-heading]]:pt-5 **:[[cmdk-group-heading]]:pb-3",
                  "**:[[cmdk-group-heading]]:text-sm **:[[cmdk-group-heading]]:font-medium",
                  "**:[[cmdk-group-heading]]:text-op-header-search-text"
                )}
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
