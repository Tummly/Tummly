/** Figma Campaigns overview — nodes 3462:61945 (header) / 4026:45443 (true-empty). */

export const CAMPAIGNS_PAGE_COPY = {
  title: "Campaigns",
  subtitle:
    "Send permission-based email or SMS campaigns to eligible guests and track what happens next.",
  /** Campaigns list section title (true-empty card). */
  listSectionTitle: "Campaigns",
  listSectionSubtitle:
    "Review drafts, scheduled sends and campaign performance for the selected locations.",
  createCampaign: "Create campaign",
  useTemplate: "Use a template",
  trueEmptyTitle: "No campaigns yet",
  trueEmptyHelper:
    "Create your first permission-based campaign or start with a template.",
} as const

export const CAMPAIGNS_PAGE_META_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-muted-foreground"

/** True-empty action row — Figma 4026:45652 (12px gap). */
export const CAMPAIGNS_TRUE_EMPTY_ACTIONS_CLASS =
  "mt-[30px] flex items-center justify-center gap-3"

/** Helper width from Figma true-empty (4026:45651). */
export const CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[306px] text-sm font-medium leading-[18px] text-muted-foreground dark:text-[#7c7c7c]"
