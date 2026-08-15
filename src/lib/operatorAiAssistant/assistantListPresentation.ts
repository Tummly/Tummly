export const ASSISTANT_RECENT_TITLE = "Conversations"
export const ASSISTANT_ARCHIVE_TITLE = "Archive Conversations"
export const ASSISTANT_ARCHIVE_EMPTY_TITLE = "Archived Conversations"
export const ASSISTANT_BACK_TO_CONVERSATION = "Back to conversation"
export const ASSISTANT_SEARCH_PLACEHOLDER = "Search conversations"

export const ASSISTANT_EMPTY_RECENT_HEADING = "No conversations yet"
export const ASSISTANT_EMPTY_RECENT_BODY =
  "Start a conversation to analyse guest activity, prepare a draft or understand performance."
export const ASSISTANT_START_CONVERSATION = "Start a conversation"

export const ASSISTANT_SEARCH_MISS_HEADING = "No conversations match"

export const ASSISTANT_EMPTY_ARCHIVE_HEADING = "No archived conversations"
export const ASSISTANT_EMPTY_ARCHIVE_BODY =
  "Conversations you archive will appear here."

export const ASSISTANT_OFFLINE_HEADING = "You\u2019re offline"
export const ASSISTANT_OFFLINE_BODY =
  "Previously loaded conversations may remain visible, but new messages cannot be sent until the connection returns."

export const ASSISTANT_LIST_ERROR_HEADING = "Could not load conversations"
export const ASSISTANT_BODY_ERROR_HEADING = "Could not load this conversation"

export const ASSISTANT_DELETE_TITLE = "Delete this conversation?"
export const ASSISTANT_DELETE_BODY =
  "This removes the conversation and its AI messages from your history. Linked campaigns, offers, feedback records and audit history will not be deleted."
export const ASSISTANT_DELETE_CONFIRM = "Delete conversation"

export const ASSISTANT_LIST_MENU_CLASS =
  "z-[120] min-w-[180px] gap-0 overflow-hidden rounded-[4px] bg-op-assistant-list-menu-background p-0 text-op-assistant-list-title shadow-[0_0_17px_rgba(0,0,0,0.09)] ring-0"

/** Figma 4953:24017 — shared 22px page gutter. */
export const ASSISTANT_LIST_PAGE_CLASS =
  "flex min-h-0 flex-1 flex-col bg-op-assistant-list-background px-[22px] pt-[22px] pb-[22px]"

/** Figma item card — 10px inline, 12px block, 4px radius, hover fill. */
export const ASSISTANT_LIST_ROW_CLASS =
  "flex w-full cursor-pointer items-center gap-2.5 overflow-hidden rounded-[4px] px-[10px] py-[12px] transition-colors hover:bg-op-assistant-list-row-hover"

/** Date label — same 10px inline padding as the item card. */
export const ASSISTANT_LIST_GROUP_LABEL_CLASS =
  "px-[10px] text-sm font-semibold text-op-assistant-list-subtitle"

/** Figma Header/Search, mapped through Conversations light/dark tokens. */
export const ASSISTANT_LIST_SEARCH_CLASS =
  "h-10 rounded-[2px] border-0 bg-op-assistant-list-search-background pl-9 text-sm text-op-assistant-list-title shadow-none transition-colors placeholder:text-op-assistant-list-subtitle hover:bg-op-assistant-list-search-hover focus-visible:bg-op-assistant-list-search-background focus-visible:ring-0 disabled:opacity-100"

export type OperatorAiAssistantListChromeKind =
  | "rows"
  | "loading"
  | "empty-recent"
  | "search-miss"
  | "empty-archive"
  | "offline"
  | "list-error"
  | "body-error"

export function conversationCountLabel(count: number): string {
  return count === 1 ? "1 conversation" : `${count} conversations`
}
