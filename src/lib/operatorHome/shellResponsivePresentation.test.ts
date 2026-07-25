import { describe, expect, it } from "vitest"

import {
  OPERATOR_DRAWER_ACTION_ROW_CLASS,
  OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
  OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS,
  OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS,
  OPERATOR_MOBILE_NAV_SHEET_CLASS,
  OPERATOR_NOTIFICATION_FILTER_TABLIST_CLASS,
  OPERATOR_NOTIFICATION_FILTER_TAB_CLASS,
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
  OPERATOR_RIGHT_DRAWER_WIDTH_CLASS,
  OPERATOR_SHELL_GUTTER_X,
  OPERATOR_SHELL_GUTTER_Y,
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
  OPERATOR_SHELL_MENU_PANEL_FILL_CLASS,
  OPERATOR_SHELL_TOOLTIP_ARROW_CLASS,
  OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS,
  OPERATOR_SHELL_TOUCH_TARGET_CLASS,
} from "./shellResponsivePresentation"

describe("shellResponsivePresentation", () => {
  it("uses stepped main-pane horizontal gutters from the responsiveness PRD", () => {
    expect(OPERATOR_SHELL_GUTTER_X).toBe(
      "px-4 sm:px-6 md:px-8 lg:px-[70px]"
    )
  })

  it("uses stepped main-pane vertical padding from the responsiveness PRD", () => {
    expect(OPERATOR_SHELL_GUTTER_Y).toBe(
      "pt-6 pb-10 md:pt-8 lg:pt-[70px] lg:pb-[70px]"
    )
  })

  it("caps the compact location switcher below lg", () => {
    expect(OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS).toContain(
      "max-w-[9rem]"
    )
    expect(OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS).toContain(
      "sm:max-w-[11rem]"
    )
    expect(OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS).toContain(
      "md:max-w-[14rem]"
    )
    expect(OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS).toContain("min-w-0")
    expect(OPERATOR_LOCATION_SWITCHER_COMPACT_WIDTH_CLASS).toContain("shrink")
  })

  it("sizes the full location switcher to content at lg", () => {
    expect(OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS).toContain("lg:w-auto")
    expect(OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS).toContain(
      "lg:max-w-none"
    )
    expect(OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS).toContain(
      "lg:shrink-0"
    )
  })

  it("uses compact touch targets for shell icon buttons", () => {
    expect(OPERATOR_SHELL_TOUCH_TARGET_CLASS).toBe(
      "size-9 min-h-9 min-w-9 p-0"
    )
  })

  it("uses shared menu panel chrome (sharp radius, panel fill, no ring)", () => {
    expect(OPERATOR_SHELL_MENU_PANEL_CLASS).toContain("rounded-xs")
    expect(OPERATOR_SHELL_MENU_PANEL_CLASS).toContain("bg-[#ebebeb]")
    expect(OPERATOR_SHELL_MENU_PANEL_CLASS).toContain("dark:bg-[#202020]")
    expect(OPERATOR_SHELL_MENU_PANEL_CLASS).toContain("ring-0")
  })

  it("exposes chrome and fill as separate exports", () => {
    expect(OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS).toContain("rounded-xs")
    expect(OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS).toContain("ring-0")
    expect(OPERATOR_SHELL_MENU_PANEL_CHROME_CLASS).not.toContain("bg-[#ebebeb]")
    expect(OPERATOR_SHELL_MENU_PANEL_FILL_CLASS).toContain("bg-[#ebebeb]")
  })

  it("uses operator surface/text tokens for tooltip chrome", () => {
    expect(OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS).toContain(
      "bg-op-surface-primary"
    )
    expect(OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS).toContain("text-op-text-primary")
    expect(OPERATOR_SHELL_TOOLTIP_ARROW_CLASS).toContain(
      "bg-op-surface-primary"
    )
    expect(OPERATOR_SHELL_TOOLTIP_ARROW_CLASS).toContain(
      "fill-op-surface-primary"
    )
  })

  it("uses square shell menu item rows with soft hover wash", () => {
    expect(OPERATOR_SHELL_MENU_ITEM_CLASS).toContain("rounded-none")
    expect(OPERATOR_SHELL_MENU_ITEM_CLASS).toContain("px-3")
    expect(OPERATOR_SHELL_MENU_ITEM_CLASS).toContain("py-3")
    expect(OPERATOR_SHELL_MENU_ITEM_CLASS).toContain("hover:bg-black/5")
    expect(OPERATOR_SHELL_MENU_ITEM_CLASS).toContain("focus-visible:bg-black/5")
    expect(OPERATOR_SHELL_MENU_ITEM_CLASS).not.toContain("focus:bg-black/5")
    expect(OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS).toContain("text-primary")
    expect(OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS).toContain("bg-transparent")
    expect(OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS).not.toContain("border-primary")
  })

  it("uses full-width mobile nav sheet below lg", () => {
    expect(OPERATOR_MOBILE_NAV_SHEET_CLASS).toBe(
      "data-[side=left]:w-full data-[side=left]:max-w-none data-[side=left]:sm:max-w-none"
    )
  })

  it("uses full-bleed mobile width capped at 481px", () => {
    expect(OPERATOR_RIGHT_DRAWER_WIDTH_CLASS).toContain(
      "data-[vaul-drawer-direction=right]:w-[min(481px,100vw)]"
    )
    expect(OPERATOR_RIGHT_DRAWER_WIDTH_CLASS).toContain(
      "data-[vaul-drawer-direction=right]:sm:max-w-[481px]"
    )
    expect(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS).toContain(
      OPERATOR_RIGHT_DRAWER_WIDTH_CLASS
    )
    expect(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS).toContain("overflow-hidden")
  })

  it("keeps drawer bodies scrollable under a fixed header", () => {
    expect(OPERATOR_RIGHT_DRAWER_BODY_CLASS).toContain("overflow-y-auto")
    expect(OPERATOR_RIGHT_DRAWER_BODY_CLASS).toContain("min-h-0")
  })

  it("scrolls notification filter tabs horizontally with 44px hit area below md", () => {
    expect(OPERATOR_NOTIFICATION_FILTER_TABLIST_CLASS).toContain(
      "overflow-x-auto"
    )
    expect(OPERATOR_NOTIFICATION_FILTER_TABLIST_CLASS).toContain("flex-1")
    expect(OPERATOR_NOTIFICATION_FILTER_TAB_CLASS).toContain("min-h-11")
    expect(OPERATOR_NOTIFICATION_FILTER_TAB_CLASS).toContain("md:min-h-0")
    expect(OPERATOR_NOTIFICATION_FILTER_TAB_CLASS).toContain("shrink-0")
  })

  it("uses Figma dark panel fill and sharp left radius for right drawers", () => {
    expect(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS).toContain("dark:bg-[#202020]")
    expect(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS).toContain(
      "data-[vaul-drawer-direction=right]:rounded-l-[2px]"
    )
  })

  it("wraps drawer actions with touch-friendly height below md", () => {
    expect(OPERATOR_DRAWER_ACTION_ROW_CLASS).toContain("flex-wrap")
    expect(OPERATOR_DRAWER_PRIMARY_ACTION_CLASS).toContain("min-h-11")
    expect(OPERATOR_DRAWER_PRIMARY_ACTION_CLASS).toContain("md:min-h-[37px]")
  })
})
