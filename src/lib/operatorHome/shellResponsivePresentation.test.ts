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

  it("restores the full 305px switcher at lg", () => {
    expect(OPERATOR_LOCATION_SWITCHER_FULL_WIDTH_CLASS).toBe("lg:w-[305px]")
  })

  it("uses compact touch targets for shell icon buttons", () => {
    expect(OPERATOR_SHELL_TOUCH_TARGET_CLASS).toBe(
      "size-9 min-h-9 min-w-9 p-0"
    )
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

  it("wraps drawer actions with touch-friendly height below md", () => {
    expect(OPERATOR_DRAWER_ACTION_ROW_CLASS).toContain("flex-wrap")
    expect(OPERATOR_DRAWER_PRIMARY_ACTION_CLASS).toContain("min-h-11")
    expect(OPERATOR_DRAWER_PRIMARY_ACTION_CLASS).toContain("md:min-h-[37px]")
  })
})
