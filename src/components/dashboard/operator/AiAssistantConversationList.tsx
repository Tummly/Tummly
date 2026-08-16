import {
  ChevronRightIcon,
  MoreVerticalIcon,
  PlusCircleIcon,
} from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { AiIcon } from "@/components/ui/ai-icon"
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
  ASSISTANT_LIST_GROUP_LABEL_CLASS,
  ASSISTANT_LIST_MENU_CLASS,
  ASSISTANT_LIST_PAGE_CLASS,
  ASSISTANT_LIST_ROW_CLASS,
  ASSISTANT_LIST_SEARCH_CLASS,
  ASSISTANT_SEARCH_PLACEHOLDER,
  ASSISTANT_START_CONVERSATION,
} from "@/lib/operatorAiAssistant/assistantListPresentation"
import type {
  OperatorAiAssistantPresentedRow,
  OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
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
  expandedSidebar?: boolean
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
        ASSISTANT_LIST_ROW_CLASS,
        row.isCurrent
          && "bg-op-assistant-list-row-active hover:bg-op-assistant-list-row-active"
      )}
    >
      <button
        type="button"
        className="flex min-w-0 flex-1 cursor-pointer flex-col items-start gap-1 text-left"
        onClick={() => {
          onOpen(row.id)
        }}
      >
        <span
          className={cn(
            "w-full truncate text-sm font-medium",
            row.isCurrent
              ? "text-op-assistant-list-title"
              : "text-op-assistant-list-subtitle"
          )}
        >
          {row.title}
        </span>
        <span className="w-full truncate text-xs font-normal text-op-assistant-list-subtitle">
          {row.meta}
        </span>
      </button>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-11 shrink-0 text-op-assistant-list-subtitle hover:bg-transparent md:size-8"
            aria-label={`Actions for ${row.title}`}
            onClick={(event) => {
              event.stopPropagation()
            }}
          >
            <MoreVerticalIcon className="size-4" aria-hidden />
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
  expandedSidebar = false,
}: AiAssistantConversationListProps) {
  const isArchive = snapshot.listPanel === "archive"
  const hasOfflineRows =
    snapshot.listChromeKind === "offline" && snapshot.listRows.length > 0
  const showCenterChrome =
    snapshot.listChromeKind !== "rows" && !hasOfflineRows

  return (
    <div
      className={cn(
        ASSISTANT_LIST_PAGE_CLASS,
        expandedSidebar
          && "w-[332px] flex-none border-x border-op-assistant-list-border px-0"
      )}
    >
      <div
        className={cn(
          "flex shrink-0 items-center justify-between pb-[22px]",
          expandedSidebar && "px-5 pb-[21px]"
        )}
      >
        {expandedSidebar ? (
          <>
            <div className="flex items-center gap-2">
              <AiIcon size={32} />
              <p className="text-base font-medium text-op-assistant-list-title">
                AI Assistant
              </p>
            </div>
            <Button
              type="button"
              variant="op-ghost"
              className="h-auto min-h-11 gap-1.5 px-0 py-0 text-sm font-normal text-op-text-primary hover:bg-transparent md:min-h-0"
              onClick={onStartConversation}
            >
              <PlusCircleIcon className="size-[18px]" aria-hidden />
              New chat
            </Button>
          </>
        ) : (
          <>
            <p className="text-lg font-medium text-op-assistant-list-title">
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
          </>
        )}
      </div>

      <div className="flex min-h-0 flex-1 flex-col justify-between gap-3">
        <div className="flex min-h-0 flex-1 flex-col gap-3">
          {snapshot.showSearch ? (
            <div className={cn("relative w-full", expandedSidebar && "px-5")}>
              <OperatorSearchIcon
                className={cn(
                  "pointer-events-none absolute top-1/2 size-4 -translate-y-1/2 text-op-assistant-list-subtitle",
                  expandedSidebar ? "left-[34px]" : "left-3.5"
                )}
              />
              <Input
                value={snapshot.searchQuery}
                onChange={(event) => {
                  onSearchQueryChange(event.target.value)
                }}
                aria-label={ASSISTANT_SEARCH_PLACEHOLDER}
                placeholder={ASSISTANT_SEARCH_PLACEHOLDER}
                className={cn(
                  ASSISTANT_LIST_SEARCH_CLASS,
                  expandedSidebar && "pl-9"
                )}
              />
            </div>
          ) : null}

          {!expandedSidebar && snapshot.listCountLabel != null ? (
            <p className="text-sm font-medium text-op-assistant-list-title">
              {snapshot.listCountLabel}
            </p>
          ) : null}

          {hasOfflineRows ? (
            <div className="flex flex-col items-center gap-2.5 px-2.5 py-3 text-center">
              {snapshot.listHeading != null ? (
                <p className="text-base font-medium text-op-assistant-list-title">
                  {snapshot.listHeading}
                </p>
              ) : null}
              {snapshot.listBody != null ? (
                <p className="max-w-[321px] text-sm leading-[18px] font-medium text-op-assistant-list-subtitle">
                  {snapshot.listBody}
                </p>
              ) : null}
            </div>
          ) : null}

          {showCenterChrome ? (
            <div className="flex flex-1 flex-col items-center justify-center">
              <div className="flex max-w-[321px] flex-col items-center gap-4 text-center">
                {snapshot.listHeading != null ? (
                  <div className="flex flex-col items-center gap-2.5">
                    <p className="text-base font-medium text-op-assistant-list-title">
                      {snapshot.listHeading}
                    </p>
                    {snapshot.listBody != null ? (
                      <p className="text-sm leading-[18px] font-medium text-op-assistant-list-subtitle">
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
            <div
              className={cn(
                "flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto py-5",
                expandedSidebar && "px-2.5"
              )}
            >
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
                  className={cn(
                    "flex flex-col gap-2 border-b border-op-assistant-list-border py-5 first:pt-2.5 last:border-b-0",
                    expandedSidebar && "px-2.5"
                  )}
                >
                  <p className={ASSISTANT_LIST_GROUP_LABEL_CLASS}>
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
          <div className={cn(expandedSidebar && "px-5")}>
            <Button
              type="button"
              variant="op-tertiary"
              className="h-[42px] self-start px-[17px]"
              onClick={onOpenArchive}
            >
              Archive
            </Button>
          </div>
        ) : null}
      </div>
    </div>
  )
}
