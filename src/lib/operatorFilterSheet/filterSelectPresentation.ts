/**
 * Shared Operator Filter Select field chrome — trigger + menu + list rows.
 * Used by Filter Sheet and Shop form selects / dropdowns.
 */

import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

/**
 * Transparent fill so the field matches dialog #1B1B1B / white — not outline’s muted wash.
 * `!h-[50px]` beats `op-ghost`’s compound `!h-auto` so horizontal padding still reads correctly.
 */
export const FILTER_SELECT_TRIGGER_CLASS =
  "!h-[50px] !min-h-[50px] w-full justify-between rounded border border-input bg-transparent px-[15px] text-left text-sm font-normal shadow-none hover:bg-transparent aria-expanded:bg-transparent dark:bg-transparent dark:hover:bg-transparent dark:aria-expanded:bg-transparent"

/** Panel chrome shared by Popover and Radix Select menus. */
export const FILTER_SELECT_MENU_CHROME_CLASS = cn(
  "gap-0 p-0",
  OPERATOR_SHELL_MENU_PANEL_CLASS,
  "bg-op-background-primary"
)

/**
 * Select field Popover menus — light `#EDEFEE` ≈ `--op-background-primary`;
 * dark keeps panel `#202020`. Width matches the Popover trigger.
 */
export const FILTER_SELECT_MENU_CLASS = cn(
  "z-[140] w-[var(--radix-popover-trigger-width)]",
  FILTER_SELECT_MENU_CHROME_CLASS
)

/**
 * Radix SelectContent menus — same chrome; width comes from `position="popper"`.
 * Pass an extra `z-*` when portaled above a dialog/sheet.
 */
export const FILTER_SELECT_CONTENT_CLASS = cn(
  "z-[140]",
  FILTER_SELECT_MENU_CHROME_CLASS
)

export const FILTER_SELECT_LIST_CLASS =
  "flex flex-col divide-y divide-op-border-default"

export const FILTER_SELECT_ITEM_CLASS = cn(
  "h-auto w-full justify-start text-left text-sm font-normal text-foreground",
  OPERATOR_SHELL_MENU_ITEM_CLASS
)

export const FILTER_SELECT_ITEM_ACTIVE_CLASS =
  OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS

export const FILTER_SELECT_ITEM_MUTED_CLASS = cn(
  "h-auto w-full justify-start text-left text-sm text-muted-foreground",
  OPERATOR_SHELL_MENU_ITEM_CLASS
)

export const FILTER_SELECT_ITEM_NAV_CLASS = cn(
  "h-auto w-full justify-start text-left text-xs font-medium text-muted-foreground",
  OPERATOR_SHELL_MENU_ITEM_CLASS
)

/** Placeholder / empty trigger text colour (Filter Sheet “Select”). */
export const FILTER_SELECT_PLACEHOLDER_CLASS = "text-[#7d7d7d]"
