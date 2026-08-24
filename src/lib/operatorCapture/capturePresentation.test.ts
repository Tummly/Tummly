import { describe, expect, it } from "vitest"

import { CAPTURE_CONNECTED_OFFERS_STUB } from "./buildCaptureGuestExperience"
import {
  CAPTURE_BREADCRUMB_LINK_CLASS,
  CAPTURE_BREADCRUMB_MUTED_LINK_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS,
  CAPTURE_GUEST_FORM_STUB,
  CAPTURE_GUEST_PREVIEW_DEVICE,
  CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS,
  CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS,
  CAPTURE_GUEST_PREVIEW_FRAME_CLASS,
  CAPTURE_GUEST_PREVIEW_BODY_CLASS,
  CAPTURE_GUEST_PREVIEW_META_ROW_CLASS,
  CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS,
  CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS,
  CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS,
  CAPTURE_GUEST_PREVIEW_PAGE_TAB,
  CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS,
  CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS,
  CAPTURE_GUEST_PREVIEW_SHELL_MOBILE_CONTENT_CLASS,
  CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
  CAPTURE_GUEST_PREVIEW_TITLE_STACK_CLASS,
  CAPTURE_MATERIALS_INNER_TITLE_CLASS,
  CAPTURE_PAGE_TITLE_CLASS,
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_FIELD_TRIGGER_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
  CAPTURE_DIALOG_SELECT_ITEM_CLASS,
  CAPTURE_DIALOG_SELECT_MENU_CLASS,
  CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_CONTENT_CLASS,
  CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_DESCRIPTION_CLASS,
  CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_TITLE_CLASS,
  CAPTURE_KPI_CELL_CLASS,
  CAPTURE_KPI_DIVIDER_CLASS,
  CAPTURE_KPI_ROW_CLASS,
  CAPTURE_OVERVIEW_KPI_DIVIDER_CLASS,
  CAPTURE_OVERVIEW_KPI_ROW_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_BODY_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_LABEL_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS,
  OPERATOR_CAPTURE_GUEST_PREVIEW_COPY,
  OPERATOR_CAPTURE_GUEST_PREVIEW_PICKER_COPY,
  OPERATOR_CAPTURE_PLACEMENTS_COLUMNS,
  OPERATOR_CAPTURE_SECTION_COPY,
} from "./capturePresentation"
import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
  OPERATOR_TABLE_ROW_ACTIONS_ITEM_CLASS,
  OPERATOR_TABLE_ROW_ACTIONS_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"

describe("capturePresentation — operator token audit", () => {
  it("prefers text-op-card-title-color over remapped text-foreground for titles", () => {
    for (const titleClass of [
      CAPTURE_PAGE_TITLE_CLASS,
      CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
    ]) {
      expect(titleClass).toContain("text-op-card-title-color")
      expect(titleClass).not.toContain("text-foreground")
    }
  })

  it("prefers text-op-text-primary over remapped text-foreground for body/value copy", () => {
    for (const bodyClass of [
      CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS,
      CAPTURE_MATERIALS_INNER_TITLE_CLASS,
      CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS,
      CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS,
      CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
      CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
      CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
      CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
    ]) {
      expect(bodyClass).toContain("text-op-text-primary")
      expect(bodyClass).not.toContain("text-foreground")
    }

    expect(CAPTURE_EMPTY_TITLE_CLASS).toContain("text-op-empty-title-color")
    expect(CAPTURE_EMPTY_TITLE_CLASS).not.toContain("text-foreground")
  })

  it("prefers explicit op text tokens for breadcrumb links over remapped text-foreground", () => {
    expect(CAPTURE_BREADCRUMB_LINK_CLASS).toContain("text-op-text-primary")
    expect(CAPTURE_BREADCRUMB_LINK_CLASS).not.toContain("text-foreground")
    expect(CAPTURE_BREADCRUMB_MUTED_LINK_CLASS).toContain("text-op-text-muted")
    expect(CAPTURE_BREADCRUMB_MUTED_LINK_CLASS).not.toContain("text-foreground")
  })

  it("uses Figma table ⋮ Actions chrome for the placements row actions menu", () => {
    expect(CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS).toContain(
      OPERATOR_TABLE_ROW_ACTIONS_PANEL_CLASS
    )
    expect(CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS).toBe(
      OPERATOR_TABLE_ROW_ACTIONS_ITEM_CLASS
    )
  })

  it("keeps shell chrome for Capture dialog Select menus (not table ⋮ chrome)", () => {
    expect(CAPTURE_DIALOG_SELECT_MENU_CLASS).toContain(
      OPERATOR_SHELL_MENU_PANEL_CLASS
    )
    expect(CAPTURE_DIALOG_SELECT_MENU_CLASS).not.toContain(
      OPERATOR_TABLE_ROW_ACTIONS_PANEL_CLASS
    )
    expect(CAPTURE_DIALOG_SELECT_MENU_CLASS).toContain("z-[130]")
    expect(CAPTURE_DIALOG_SELECT_ITEM_CLASS).toContain(
      OPERATOR_SHELL_MENU_ITEM_CLASS
    )
  })

  it("defines Figma-aligned Capture dialog close chrome (op-collapse + header row)", () => {
    expect(CAPTURE_DIALOG_CLOSE_BUTTON_CLASS).toContain("shrink-0")
    expect(CAPTURE_DIALOG_HEADER_ROW_CLASS).toContain("gap-[22px]")
  })

  it("keeps the row actions trigger a plain icon-sized hit area for the op-ghost variant", () => {
    expect(CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS).toBe("size-8")
  })

  it("defines presentational preview chrome copy for issue 23", () => {
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.guestFormValue).toBe(
      CAPTURE_GUEST_FORM_STUB
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.connectedOfferLabel).toBe(
      "Connected offer"
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.openPreviewInNewTabCta).toBe(
      "Open preview in new tab"
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.feedbackPageTab).toBe(
      "Feedback page"
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.thankYouPageTab).toBe(
      "Thank you page"
    )
  })

  it("locks preview tab and device values for the overlay chrome", () => {
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TAB.feedback).toBe("feedback")
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TAB.thankYou).toBe("thank-you")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE.desktop).toBe("desktop")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE.mobile).toBe("mobile")
    expect(CAPTURE_CONNECTED_OFFERS_STUB).toBe("No active offers")
  })

  it("aligns preview toolbar chrome with Figma segmented tabs and freestanding device toggles", () => {
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS).toContain("rounded-op-lg")
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS).toContain("p-3")
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS).toContain(
      "bg-op-surface-primary"
    )
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS).toContain(
      "border-op-card-border"
    )
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS).toContain(
      "group-data-horizontal/tabs:h-auto"
    )
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS).toContain("flex-none")
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS).toContain("py-3")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS).toContain("border-0")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS).toContain("gap-0")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS).toContain("px-[16px]")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS).toContain("py-[10px]")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS).toContain(
      "group-data-[spacing=0]/toggle-group:px-[16px]"
    )
    expect(CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS).toContain(
      "has-data-[icon=inline-start]:pl-[16px]"
    )
    expect(CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS).toContain("max-w-[393px]")
    expect(CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS).toContain("min-h-dvh")
    expect(CAPTURE_GUEST_PREVIEW_SHELL_MOBILE_CONTENT_CLASS).toContain(
      "sm:max-w-[min(100%,393px)]"
    )
  })

  it("aligns guest experience preview spacing and frame with Figma 4855:103166", () => {
    expect(CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS).toContain("bg-op-surface-primary")
    expect(CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS).not.toContain(
      "bg-op-background-primary"
    )
    expect(CAPTURE_GUEST_PREVIEW_TITLE_STACK_CLASS).toContain("gap-2")
    expect(CAPTURE_GUEST_PREVIEW_META_ROW_CLASS).toContain("gap-3")
    expect(CAPTURE_GUEST_PREVIEW_FRAME_CLASS).toContain("rounded-[12px]")
    expect(CAPTURE_GUEST_PREVIEW_FRAME_CLASS).toContain("border-[6px]")
    expect(CAPTURE_GUEST_PREVIEW_FRAME_CLASS).toContain(
      "border-[var(--op-color-gray-1000)]"
    )
    expect(CAPTURE_GUEST_PREVIEW_FRAME_CLASS).toContain("bg-guest-feedback-bg")
    expect(CAPTURE_GUEST_PREVIEW_FRAME_CLASS).toContain("overflow-clip")
  })

  it("styles the preview body notch with Main Bg and card border", () => {
    expect(CAPTURE_GUEST_PREVIEW_BODY_CLASS).toContain("bg-op-background-primary")
    expect(CAPTURE_GUEST_PREVIEW_BODY_CLASS).toContain("border-op-card-border")
    expect(CAPTURE_GUEST_PREVIEW_BODY_CLASS).toContain("border-t")
    expect(CAPTURE_GUEST_PREVIEW_BODY_CLASS).toContain("rounded-t-[20px]")
  })

  it("defines Preview picker mixed copy from grilling 10", () => {
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_PICKER_COPY).toEqual({
      title: "Select a placement or digital guest link",
      description:
        "Choose the physical QR placement or digital guest link you want to preview. Each may use a different guest form or offer.",
      fieldLabel: "Placements & digital links",
      placeholder: "Select placement or link",
      confirmCta: "Preview selected",
      cancelCta: "Cancel",
    })
  })

  it("uses theme-aware dialog surface tokens for the Preview picker", () => {
    expect(CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_CONTENT_CLASS).toContain(
      "bg-op-surface-secondary"
    )
    expect(CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_CONTENT_CLASS).not.toContain(
      "op-color-gray-995"
    )
    expect(CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_TITLE_CLASS).toContain(
      "text-op-text-primary"
    )
    expect(CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_DESCRIPTION_CLASS).toContain(
      "text-op-text-muted"
    )
    expect(CAPTURE_DIALOG_FIELD_TRIGGER_CLASS).toContain("border-op-input-border")
    expect(CAPTURE_DIALOG_FIELD_TRIGGER_CLASS).toContain("text-op-text-primary")
  })

  it("uses theme-aware dialog surface tokens for Pause / Activate confirm", () => {
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS).toContain(
      "bg-op-surface-secondary"
    )
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS).toContain(
      "text-op-text-primary"
    )
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS).not.toContain(
      "op-color-gray-995"
    )
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS).not.toContain(
      "text-white"
    )
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS).toContain(
      "text-op-text-primary"
    )
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_BODY_CLASS).toContain(
      "text-op-text-muted"
    )
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_LABEL_CLASS).toContain(
      "text-op-text-primary"
    )
    expect(CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS).toContain(
      "text-op-text-secondary"
    )
  })

  it("defines Figma Capture performance and overview KPI strip chrome", () => {
    expect(CAPTURE_KPI_ROW_CLASS).toContain("lg:flex")
    expect(CAPTURE_KPI_DIVIDER_CLASS).toContain("bg-op-card-border")
    expect(CAPTURE_KPI_CELL_CLASS).not.toContain("bg-op-background-primary")
    expect(CAPTURE_OVERVIEW_KPI_ROW_CLASS).toContain("lg:flex")
    expect(CAPTURE_OVERVIEW_KPI_ROW_CLASS).toContain("lg:items-start")
    expect(CAPTURE_OVERVIEW_KPI_DIVIDER_CLASS).toContain("lg:block")
    expect(CAPTURE_OVERVIEW_KPI_DIVIDER_CLASS).toContain("bg-op-card-border")
  })

  it("defines Digital guest links empty chrome and Guest form opens column labels", () => {
    expect(OPERATOR_CAPTURE_SECTION_COPY.digitalGuestLinks).toEqual({
      title: "Digital guest links",
      description:
        "Create and track digital links that can be shared across your online channels.",
      emptyTitle: "No digital guest links yet",
      emptyHelper:
        "Create a digital guest link to share across your online channels. Performance for each link will show up here.",
      createCta: "Create digital guest link",
    })
    expect(OPERATOR_CAPTURE_SECTION_COPY.performance.description).toContain(
      "opening a guest form"
    )
    expect(OPERATOR_CAPTURE_SECTION_COPY.performance.emptyHelper).toContain(
      "guest form opens"
    )
    expect(OPERATOR_CAPTURE_PLACEMENTS_COLUMNS.qrScans).toBe("Guest form opens")
  })
})
