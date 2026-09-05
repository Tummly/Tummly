/**
 * Shell navbar AI credits popover chrome — Figma 5216:26967.
 */

import {
  OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
} from "@/components/dashboard/operator/ShellUtilityChrome"
import { cn } from "@/lib/utils"

export const SHELL_AI_CREDITS_POPOVER_WIDTH_CLASS =
  "w-[min(471px,calc(100vw-1rem))]"

export const SHELL_AI_CREDITS_POPOVER_CONTENT_CLASS = cn(
  SHELL_AI_CREDITS_POPOVER_WIDTH_CLASS,
  "z-[60] gap-0 overflow-hidden rounded-op-sm border border-op-card-border",
  "bg-op-card-background p-0 text-op-text-primary shadow-md ring-0"
)

export const SHELL_AI_CREDITS_TRIGGER_CLASS = cn(
  "hidden shrink-0 gap-1.5 rounded-op-sm px-2.5 text-xs font-medium md:inline-flex",
  OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
  "lg:h-10 lg:min-h-10 lg:px-3.5 lg:text-sm"
)

export const SHELL_AI_CREDITS_BODY_CLASS =
  "flex w-full flex-col gap-[17px] p-5"

export const SHELL_AI_CREDITS_TITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-primary"

export const SHELL_AI_CREDITS_USED_LINE_CLASS =
  "m-0 text-xs font-normal leading-normal text-[var(--op-color-gray-550)]"

export const SHELL_AI_CREDITS_METER_TRACK_CLASS =
  "relative h-1.5 w-full overflow-hidden rounded-[4px] bg-op-card-border"

export const SHELL_AI_CREDITS_METER_FILL_CLASS =
  "absolute top-1/2 left-0 h-2 -translate-y-1/2 rounded-[4px] bg-[var(--op-color-green-500)]"

export const SHELL_AI_CREDITS_LEFT_LINE_CLASS =
  "m-0 text-[10px] font-normal leading-normal text-op-text-primary"

export const SHELL_AI_CREDITS_FOOTER_CLASS =
  "flex w-full flex-wrap items-center justify-end gap-3 border-t border-op-card-border p-5"
