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
  "z-[120] min-w-[180px] gap-0 overflow-hidden rounded-[4px] bg-op-surface-primary p-0 text-[#171717] shadow-[0_0_17px_rgba(0,0,0,0.09)] ring-0 dark:bg-[#202020] dark:text-white"

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
