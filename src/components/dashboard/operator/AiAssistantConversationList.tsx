import { ChevronRightIcon, MoreHorizontalIcon } from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import {
  ASSISTANT_BACK_TO_CONVERSATION,
  ASSISTANT_LIST_MENU_CLASS,
  ASSISTANT_SEARCH_PLACEHOLDER,
  ASSISTANT_START_CONVERSATION,
} from "@/lib/operatorAiAssistant/assistantListPresentation"
import type {
  OperatorAiAssistantPresentedRow,
  OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import { GUESTS_SEARCH_FIELD_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

type AiAssistantConversationListProps = {
  snapshot: OperatorAiAssistantSnapshot
  onBackToConversation: () => void
  onSearchQueryChange: (query: string) => void
  onOpenConversation: (conversationId: string) => void
  onArchive: (conversationId: string) => void
  onUnarchive: (conversationId: string) => void
  onRequestDelete: (conversationId: string) => void
  onOpenArchive: () => void
  onStartConversation: () => void
  onRetry: () => void
}

function ConversationRow({
  row,
  archived,
  onOpen,
  onArchive,
  onUnarchive,
  onRequestDelete,
}: {
  row: OperatorAiAssistantPresentedRow
  archived: boolean
  onOpen: (conversationId: string) => void
  onArchive: (conversationId: string) => void
  onUnarchive: (conversationId: string) => void
  onRequestDelete: (conversationId: string) => void
}) {
  return (
    <div
      className={cn(
        "flex w-full items-center gap-2.5 overflow-hidden rounded-[4px] px-2.5 py-3",
        row.isCurrent && "bg-[var(--op-color-gray-990)]"
      )}
    >
      <button
        type="button"
        className="flex min-w-0 flex-1 flex-col items-start gap-1 text-left"
        onClick={() => {
          onOpen(row.id)
        }}
      >
        <span
          className={cn(
            "w-full truncate text-sm font-medium",
            row.isCurrent
              ? "text-[color:var(--main-bg\/title,white)]"
              : "text-[color:var(--main-bg\/subtitle,#7c7c7c)]"
          )}
        >
          {row.title}
        </span>
        <span className="w-full truncate text-xs font-normal text-[color:var(--main-bg\/subtitle,#7c7c7c)]">
          {row.meta}
        </span>
      </button>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-11 shrink-0 text-[color:var(--main-bg\/subtitle,#7c7c7c)] hover:bg-transparent md:size-8"
            aria-label={`Actions for ${row.title}`}
            onClick={(event) => {
              event.stopPropagation()
            }}
          >
            <MoreHorizontalIcon className="size-4" aria-hidden />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className={ASSISTANT_LIST_MENU_CLASS}>
          {archived ? (
            <DropdownMenuItem
              className="rounded-none px-3.5 py-3.5 text-sm"
              onClick={() => {
                onUnarchive(row.id)
              }}
            >
              Unarchive
            </DropdownMenuItem>
          ) : (
            <DropdownMenuItem
              className="rounded-none px-3.5 py-3.5 text-sm"
              onClick={() => {
                onArchive(row.id)
              }}
            >
              Archive
            </DropdownMenuItem>
          )}
          <DropdownMenuItem
            variant="destructive"
            className="rounded-none px-3.5 py-3.5 text-sm"
            onClick={() => {
              onRequestDelete(row.id)
            }}
          >
            Delete
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}

/** Recent / Archive list chrome. Figma 4953:24017 / 4953:25309 / 3458:57040. */
export function AiAssistantConversationList({
  snapshot,
  onBackToConversation,
  onSearchQueryChange,
  onOpenConversation,
  onArchive,
  onUnarchive,
  onRequestDelete,
  onOpenArchive,
  onStartConversation,
  onRetry,
}: AiAssistantConversationListProps) {
  const isArchive = snapshot.view === "archive"
  const showCenterChrome = snapshot.listChromeKind !== "rows"

  return (
    <div className="flex min-h-0 flex-1 flex-col px-[22px] pt-[22px] pb-[22px]">
      <div className="flex shrink-0 items-start justify-between pb-[22px]">
        <p className="text-lg font-medium text-[color:var(--main-bg\/title,white)]">
          {snapshot.listTitle}
        </p>
        <Button
          type="button"
          variant="op-ghost"
          className="h-auto min-h-11 gap-2 px-0 py-0 text-sm font-medium text-op-text-primary hover:bg-transparent md:min-h-0"
          onClick={onBackToConversation}
        >
          {ASSISTANT_BACK_TO_CONVERSATION}
          <ChevronRightIcon className="size-4" aria-hidden />
        </Button>
      </div>

      <div className="flex min-h-0 flex-1 flex-col justify-between gap-3">
        <div className="flex min-h-0 flex-1 flex-col gap-3">
          {snapshot.showSearch ? (
            <div className="relative w-full">
              <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-header-search-text" />
              <Input
                value={snapshot.searchQuery}
                onChange={(event) => {
                  onSearchQueryChange(event.target.value)
                }}
                aria-label={ASSISTANT_SEARCH_PLACEHOLDER}
                placeholder={ASSISTANT_SEARCH_PLACEHOLDER}
                className={GUESTS_SEARCH_FIELD_CLASS}
              />
            </div>
          ) : null}

          {snapshot.listCountLabel != null ? (
            <p className="text-sm font-medium text-[color:var(--main-bg\/title,white)]">
              {snapshot.listCountLabel}
            </p>
          ) : null}

          {showCenterChrome ? (
            <div className="flex flex-1 flex-col items-center justify-center">
              <div className="flex max-w-[321px] flex-col items-center gap-4 text-center">
                {snapshot.listHeading != null ? (
                  <div className="flex flex-col items-center gap-2.5">
                    <p className="text-base font-medium text-[color:var(--main-bg\/title,white)]">
                      {snapshot.listHeading}
                    </p>
                    {snapshot.listBody != null ? (
                      <p className="text-sm leading-[18px] font-medium text-[color:var(--main-bg\/subtitle,#7c7c7c)]">
                        {snapshot.listBody}
                      </p>
                    ) : null}
                  </div>
                ) : null}
                {snapshot.showStartConversation ? (
                  <Button
                    type="button"
                    variant="op-secondary"
                    className="h-auto px-4 py-2.5"
                    onClick={onStartConversation}
                  >
                    {ASSISTANT_START_CONVERSATION}
                  </Button>
                ) : null}
                {snapshot.showListRetry ? (
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className="h-[42px] px-[17px]"
                    onClick={onRetry}
                  >
                    Retry
                  </Button>
                ) : null}
              </div>
            </div>
          ) : isArchive ? (
            <div className="flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto py-5">
              {snapshot.archiveRows.map((row) => (
                <ConversationRow
                  key={row.id}
                  row={row}
                  archived
                  onOpen={onOpenConversation}
                  onArchive={onArchive}
                  onUnarchive={onUnarchive}
                  onRequestDelete={onRequestDelete}
                />
              ))}
            </div>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col overflow-y-auto">
              {snapshot.recentGroups.map((group) => (
                <div
                  key={group.id}
                  className="flex flex-col gap-2 border-b border-[var(--op-color-gray-980)] py-5 first:pt-2.5 last:border-b-0"
                >
                  <p className="px-2.5 text-sm font-semibold text-[color:var(--main-bg\/subtitle,#7c7c7c)]">
                    {group.label}
                  </p>
                  {group.rows.map((row) => (
                    <ConversationRow
                      key={row.id}
                      row={row}
                      archived={false}
                      onOpen={onOpenConversation}
                      onArchive={onArchive}
                      onUnarchive={onUnarchive}
                      onRequestDelete={onRequestDelete}
                    />
                  ))}
                </div>
              ))}
            </div>
          )}
        </div>

        {snapshot.showArchiveFooter ? (
          <Button
            type="button"
            variant="op-tertiary"
            className="h-[42px] self-start px-[17px]"
            onClick={onOpenArchive}
          >
            Archive
          </Button>
        ) : null}
      </div>
    </div>
  )
}
