/**
 * Operator shell + drawer responsive presentation — PRD §1–3.
 * Source: `.scratch/home-dashboard-responsiveness/PRD.md`.
 */

/** Main pane horizontal gutters — stepped toward Figma 70px at `lg`. */
export const OPERATOR_SHELL_GUTTER_X =
  "px-4 sm:px-6 md:px-8 lg:px-[70px]"

/** Main pane vertical padding — stepped toward Figma 70px at `lg`. */
export const OPERATOR_SHELL_GUTTER_Y =
  "pt-6 pb-10 md:pt-8 lg:pt-[70px] lg:pb-[70px]"

/**
 * Compact location switcher trigger (&lt;lg): name-only width cap.
 * At `lg+`, content-sized width (see FULL) so the search field can flex beside it.
 */
export const OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS =
  "min-w-0 shrink max-w-[9rem] sm:max-w-[11rem] md:max-w-[14rem]"

/** Full location switcher trigger (≥lg) — content-sized; search flexes beside it. */
export const OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS =
  "lg:w-auto lg:max-w-none lg:shrink-0"

/** Compact icon-button hit area for navbar + mobile nav sheet. */
export const OPERATOR_SHELL_TOUCH_TARGET_CLASS =
  "size-9 min-h-9 min-w-9 p-0"

/**
 * Shared dropdown / popover panel chrome — sharp radius, soft elevation, no ring
 * (Account, Performance date, Guests filter / Sort / Actions menus).
 */
export const OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS =
  "rounded-xs shadow-[0_4px_11px_rgba(0,0,0,0.06),0_18px_20px_rgba(0,0,0,0.05)] ring-0"

/**
 * Panel fill paired with chrome (Figma #ebebeb / #202020) —
 * Account, Performance date, Guests filter / Sort / Actions menus.
 */
export const OPERATOR_SHELL_MENU_PANEL_FILL_CLASS =
  "bg-[#ebebeb] text-[#171717] dark:bg-[#202020] dark:text-white"

/** Chrome + fill — Account, Performance date, Guests filter / Sort / Actions menus. */
export const OPERATOR_SHELL_MENU_PANEL_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS} ${OPERATOR_SHELL_MENU_PANEL_FILL_CLASS}`

/**
 * Tooltip surface — operator popover tokens (`--op-surface-primary` /
 * `--op-text-primary`), same spine as dialogs/popovers under `html.op`.
 */
export const OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS =
  "bg-op-surface-primary text-op-text-primary shadow-[0_4px_11px_rgba(0,0,0,0.06),0_18px_20px_rgba(0,0,0,0.05)]"

/** Tooltip arrow fill — matches {@link OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS}. */
export const OPERATOR_SHELL_TOOLTIP_ARROW_CLASS =
  "bg-op-surface-primary fill-op-surface-primary"

/**
 * Square menu rows — Account, Performance/Guests date presets, Guests Sort / Actions.
 * Hover wash only; use focus-visible so popover auto-focus does not paint the first row.
 */
export const OPERATOR_SHELL_MENU_ITEM_CLASS =
  "rounded-none px-3 py-3 hover:bg-black/5 focus-visible:bg-black/5 dark:hover:bg-white/5 dark:focus-visible:bg-white/5"

/** Selected row — primary text, no fill/border wash. */
export const OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS =
  "bg-transparent font-medium text-primary hover:bg-transparent hover:text-primary focus-visible:bg-transparent focus-visible:text-primary dark:hover:bg-transparent dark:focus-visible:bg-transparent"

/**
 * Outlined toolbar trigger — Guests Sort + Performance/Guests date filters.
 * Transparent fill, #dcdcdc / dark #393939 border, dark text (not primary white).
 * Hover: text/icons → foreground / white; border from `op-tertiary` hover token.
 */
export const OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS =
  "h-auto min-h-0 shrink-0 gap-1.5 rounded border border-[#dcdcdc] bg-transparent px-[17px] py-[11px] text-xs font-medium leading-[18px] text-[#171717] opacity-100 shadow-none hover:bg-transparent hover:text-foreground aria-expanded:bg-transparent disabled:opacity-100 dark:border-[#393939] dark:bg-transparent dark:text-[#a6a6a6] dark:hover:bg-transparent dark:hover:text-white dark:aria-expanded:bg-transparent"

/**
 * Mobile nav sheet spans the full viewport width below `lg`.
 * Must use data-side selectors to override SheetContent defaults (`w-3/4`, `sm:max-w-sm`).
 */
export const OPERATOR_MOBILE_NAV_SHEET_CLASS =
  "data-[side=left]:w-full data-[side=left]:max-w-none data-[side=left]:sm:max-w-none"

/**
 * Right drawer width — Figma 481px, full-bleed on narrow viewports.
 * Must override DrawerContent defaults (`w-3/4`, `sm:max-w-sm` ≈ 384px).
 */
export const OPERATOR_RIGHT_DRAWER_WIDTH_CLASS =
  "data-[vaul-drawer-direction=right]:w-[min(481px,100vw)] data-[vaul-drawer-direction=right]:sm:max-w-[481px]"

/**
 * Right drawer chrome — sharp 2px left radius (Figma Button/Radius), overrides
 * DrawerContent’s default `rounded-l-xl`.
 */
export const OPERATOR_RIGHT_DRAWER_CONTENT_CLASS = `h-full max-h-dvh overflow-hidden bg-white dark:bg-[#202020] data-[vaul-drawer-direction=right]:rounded-l-[2px] ${OPERATOR_RIGHT_DRAWER_WIDTH_CLASS}`

export const OPERATOR_RIGHT_DRAWER_BODY_CLASS = "min-h-0 flex-1 overflow-y-auto"

/** Notification filter tabs — single row, horizontal scroll below md. */
export const OPERATOR_NOTIFICATION_FILTER_TABLIST_CLASS =
  "flex min-w-0 flex-1 items-center overflow-x-auto"

/**
 * 44px min hit area below md (PRD touch contract).
 * Pair with `variant="op-ghost"` — text colour only, no muted background wash.
 */
export const OPERATOR_NOTIFICATION_FILTER_TAB_CLASS =
  "min-h-11 shrink-0 px-3.5 pr-4 text-sm focus-visible:border-0 focus-visible:ring-0 md:min-h-0"

/** Drawer primary actions — wrap + 44px hit area below md. */
export const OPERATOR_DRAWER_ACTION_ROW_CLASS =
  "flex flex-wrap items-center gap-3"

export const OPERATOR_DRAWER_PRIMARY_ACTION_CLASS =
  "min-h-11 md:min-h-[37px]"
