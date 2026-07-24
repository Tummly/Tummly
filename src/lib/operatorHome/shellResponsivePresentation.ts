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
 * (Account menu, Performance date, Guests filter field menus).
 */
export const OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS =
  "rounded-xs shadow-[0_4px_11px_rgba(0,0,0,0.06),0_18px_20px_rgba(0,0,0,0.05)] ring-0"

/**
 * Panel fill paired with chrome for Account + Guests filter field menus
 * (Figma #ebebeb / #202020). Omit on the Performance date trigger popover.
 */
export const OPERATOR_SHELL_MENU_PANEL_FILL_CLASS =
  "bg-[#ebebeb] text-[#171717] dark:bg-[#202020] dark:text-white"

/** Chrome + fill — Account menu and Guests filter field menus. */
export const OPERATOR_SHELL_MENU_PANEL_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS} ${OPERATOR_SHELL_MENU_PANEL_FILL_CLASS}`

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
 * Active/hover change text colour only — no muted background wash.
 */
export const OPERATOR_NOTIFICATION_FILTER_TAB_CLASS =
  "h-auto min-h-11 shrink-0 rounded-none border-0 bg-transparent px-3.5 pr-4 text-sm shadow-none hover:bg-transparent hover:text-foreground active:bg-transparent dark:hover:bg-transparent dark:active:bg-transparent focus-visible:border-0 focus-visible:ring-0 md:min-h-0"

/** Drawer primary actions — wrap + 44px hit area below md. */
export const OPERATOR_DRAWER_ACTION_ROW_CLASS =
  "flex flex-wrap items-center gap-3"

export const OPERATOR_DRAWER_PRIMARY_ACTION_CLASS =
  "min-h-11 md:min-h-[37px]"
